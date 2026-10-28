using Amazon.Lambda.Core;
using APIHandler.Constants;
using APIHandler.Entities;
using APIHandler.Exceptions;
using APIHandler.Helpers;
using APIHandler.Mappers;
using APIHandler.Models.Reservations;
using APIHandler.Models.WaiterList;
using APIHandler.Records;
using APIHandler.Repositories.Auth;
using APIHandler.Repositories.Locations;
using APIHandler.Repositories.Reservations;
using APIHandler.Services.GroupReservations;
using APIHandler.Services.Locations;
using APIHandler.Services.Orders;
using APIHandler.Services.Waiters;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace APIHandler.Services.Reservations;

public class ReservationService : IReservationService
{
    private readonly IReservationRepository _reservationRepository;
    private readonly ILocationRepository _locationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IWaiterService _waiterService;
    private readonly ILocationService _locationService;
    private readonly IOrderService _orderService;

    private readonly IGroupReservationService _groupReservationService;

    public ReservationService(
        IReservationRepository reservationRepository,
        ILocationRepository locationRepository,
        IUserRepository userRepository,
        IWaiterService waiterService,
        ILocationService locationService,
        IOrderService orderService,
        IGroupReservationService groupReservationService)
    {
        _reservationRepository = reservationRepository ?? throw new ArgumentNullException(nameof(reservationRepository));
        _locationRepository = locationRepository ?? throw new ArgumentNullException(nameof(locationRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _waiterService = waiterService ?? throw new ArgumentNullException(nameof(waiterService));
        _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        _groupReservationService = groupReservationService ?? throw new ArgumentNullException(nameof(groupReservationService));
    }

    public async Task<ReservationResponse> CreateReservationAsync(CreateReservationRequest request, string callerId, bool isWaiter, ILambdaLogger logger)
    {
        var table = await LoadAndValidateTableAsync(request.TableId);
        var location = await GetLocationByTableIdAsync(table.Id, logger);

        var reservationContext = await ResolveTargetUserIdAsync(request, callerId, isWaiter, table);

        if (!reservationContext.CreatedByWaiter)
        {
            var waiter = await GetLeastBusyWaiterForLocationAsync(table.LocationId, logger);
            reservationContext = reservationContext with { WaiterId = waiter.UserId };
        }

        var reservation = BuildReservation(request, reservationContext);
        await ValidateReservationAsync(reservation, table);

        await SaveReservationAsync(reservation, logger);
        await UpdateWaiterReservationsAsync(reservation.WaiterId, reservation.Id);

        if (request.GuestCount >= 2)
            await _groupReservationService.CreateGroupReservationAsync(reservation.Id);

        return ReservationMapper.ToReservationResponse(reservation, location.Address);
    }

    public async Task<string> GetLocationIdForReservation(string reservationId)
    {
        var reservation = await _reservationRepository.GetReservationByIdAsync(reservationId)
            ?? throw new ReservationNotFoundException($"Reservation with ID {reservationId} not found.");

        var table = await _locationRepository.GetTableByIdAsync(reservation.TableId)
            ?? throw new ValidationException($"Table with ID {reservation.TableId} not found.");

        return table.LocationId;
    }

    public async Task<Reservation?> GetReservationByIdAsync(string reservationId)
    {
        try
        {
            return await _reservationRepository.GetReservationByIdAsync(reservationId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving reservation with ID {reservationId}: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> UpdateReservationAsync(Reservation reservation)
    {
        try
        {
            await _reservationRepository.SaveReservationAsync(reservation);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating reservation with ID {reservation.Id}: {ex.Message}");
            return false;
        }

        if (reservation.CustomerId is null)
            return true;

        if (reservation.Status == ReservationStatus.Reserved)
        {
            if (reservation.GuestCount >= 2)
                await _groupReservationService.CreateGroupReservationIfDoesNotExistAsync(reservation.CustomerId, reservation.Id);

            return true;
        }

        await _groupReservationService.CloseGroupReservationAsync(reservation.Id);
        return true;
    }

    public async Task<ReservationResponse> UpdateReservationTableAsync(string callerId, string reservationId, string newTableId, ILambdaLogger logger)
    {
        var reservation = await LoadReservationAsync(reservationId)
            ?? throw new ReservationNotFoundException("Reservation not found.");

        await EnsureWaiterOwnsReservationAsync(callerId, reservation);

        var newTable = await LoadAndValidateTableAsync(newTableId);
        await EnsureWaiterAssignedToTableLocationAsync(callerId, newTable.LocationId);

        ValidateCapacity(reservation.GuestCount, newTable.Capacity);
        await EnsureNoOverlapsAsync(newTableId, reservation.StartTime, reservation.EndTime, reservation.Id);

        reservation.TableId = newTableId;
        await _reservationRepository.SaveReservationAsync(reservation);

        logger.Log($"Reservation {reservationId} table updated to {newTableId} by waiter {callerId}.");
        return await BuildReservationResponseWithDetailsAsync(reservation);
    }

    private async Task ValidateReservationAsync(Reservation reservation, Table table)
    {
        ValidateTimes(reservation);
        ValidateCapacity(reservation.GuestCount, table.Capacity);
        await EnsureNoOverlapsAsync(table.Id, reservation.StartTime, reservation.EndTime);
    }

    private async Task<Table> LoadAndValidateTableAsync(string tableId)
    {
        var table = await _locationRepository.GetTableByIdAsync(tableId);
        return table ?? throw new ValidationException($"The specified table '{tableId}' was not found.");
    }

    private async Task<ReservationContext> ResolveTargetUserIdAsync(CreateReservationRequest request, string callerId, bool isWaiter, Table table)
    {
        if (!isWaiter)
        {
            return new ReservationContext(callerId, null, false);
        }

        var waiter = await _waiterService.FindWaiterByIdAsync(callerId);
        if (waiter.LocationId != table.LocationId)
        {
            throw new ActionForbiddenException(
                $"Action forbidden: You are assigned to location '{waiter.LocationId}', but table '{table.Id}' belongs to location '{table.LocationId}'.");
        }

        if (string.IsNullOrWhiteSpace(request.CustomerEmail))
        {
            return new ReservationContext(null, callerId, true);
        }

        var user = await _userRepository.GetUserByEmailAsync(request.CustomerEmail);
        var customerId = user.UserAttributes.FirstOrDefault(a => a.Name == "sub")?.Value
            ?? throw new InvalidOperationException($"Customer with email '{request.CustomerEmail}' not found in Cognito.");
        return new ReservationContext(customerId, callerId, true);
    }

    private async Task EnsureWaiterOwnsReservationAsync(string callerId, Reservation reservation)
    {
        if (DataValidationHelper.IsMissing(reservation.WaiterId))
        {
            throw new ValidationException($"Reservation '{reservation.Id}' has no assigned waiter.");
        }

        if (EqualityHelper.NotEqualsIgnoreCase(reservation.WaiterId, callerId))
        {
            throw new ActionForbiddenException($"Action forbidden: You ({callerId}) are not the assigned waiter ({reservation.WaiterId}) for reservation '{reservation.Id}'.");
        }

        var waiter = await _waiterService.FindWaiterByIdAsync(callerId);
        var table = await LoadAndValidateTableAsync(reservation.TableId);

        if (EqualityHelper.NotEqualsIgnoreCase(waiter.LocationId, table.LocationId))
        {
            throw new ActionForbiddenException(
                $"Action forbidden: You are assigned to location '{waiter.LocationId}', but table '{table.Id}' belongs to location '{table.LocationId}'.");
        }
    }

    private static Reservation BuildReservation(CreateReservationRequest request, ReservationContext context)
        => ReservationMapper.ToReservation(request, context);

    private static void ValidateTimes(Reservation reservation)
    {
        if ((reservation.StartTime.ToUniversalTime() - DateTime.UtcNow).TotalMinutes < 15)
            throw new ValidationException($"Reservations must be made at least 15 minutes in advance. Requested start time: {reservation.StartTime:HH:mm}, Current time: {DateTime.UtcNow:HH:mm} UTC.");
        if (reservation.EndTime <= reservation.StartTime)
            throw new ValidationException($"End time ({reservation.EndTime:HH:mm}) must be after start time ({reservation.StartTime:HH:mm}).");
    }

    private static void ValidateCapacity(int guestCount, int tableCapacity)
    {
        if (guestCount > tableCapacity)
            throw new ValidationException($"Guest count ({guestCount}) exceeds table capacity ({tableCapacity}).");
    }

    private async Task EnsureNoOverlapsAsync(string tableId, DateTime startTime, DateTime endTime, string? reservationIdToExclude = null)
    {
        var reservations = await _reservationRepository.GetReservationsByTableIdAsync(tableId);
        var buffer = TimeSpan.FromMinutes(15);

        var hasOverlap = reservations.Any(r =>
            EqualityHelper.NotEqualsIgnoreCase(r.Id, reservationIdToExclude) &&
            r.EndTime > DateTime.UtcNow &&
            startTime < r.EndTime.Add(buffer) &&
            endTime > r.StartTime.Subtract(buffer));

        if (hasOverlap)
        {
            throw new ValidationException($"Reservation from {startTime:HH:mm} to {endTime:HH:mm} overlaps with an existing reservation for table '{tableId}'.");
        }
    }

    private async Task<WaiterListItem> GetLeastBusyWaiterForLocationAsync(string locationId, ILambdaLogger logger)
    {
        try
        {
            return await _waiterService.GetLeastBusyWaiterAsync(locationId);
        }
        catch (Exception ex)
        {
            logger.Log($"Error fetching least busy waiter: {ex.Message}");
            throw;
        }
    }

    private async Task<Location> GetLocationByTableIdAsync(string tableId, ILambdaLogger logger)
    {
        try
        {
            return await _locationService.GetLocationByTableIdAsync(tableId);
        }
        catch (Exception ex)
        {
            logger.Log($"Error fetching location: {ex.Message}");
            throw;
        }
    }

    private async Task SaveReservationAsync(Reservation reservation, ILambdaLogger logger)
    {
        await _reservationRepository.SaveReservationAsync(reservation);
        logger.Log($"Reservation with id: {reservation.Id} was created successfully.");
    }

    private async Task UpdateWaiterReservationsAsync(string waiterUserId, string newReservationId)
    {
        var waiter = await _waiterService.FindWaiterByIdAsync(waiterUserId);
        var updatedReservations = waiter.Reservations.Append(newReservationId);
        await _waiterService.UpdateWaiterReservationsAsync(waiterUserId, updatedReservations);
    }

    private async Task<Reservation?> LoadReservationAsync(string reservationId)
    {
        var reservation = await _reservationRepository.GetReservationByIdAsync(reservationId);
        return reservation ?? throw new ReservationNotFoundException($"Reservation '{reservationId}' not found.");
    }

    private async Task EnsureWaiterAssignedToTableLocationAsync(string waiterUserId, string tableLocationId)
    {
        var waiter = await _waiterService.FindWaiterByIdAsync(waiterUserId);

        if (EqualityHelper.NotEqualsIgnoreCase(waiter.LocationId, tableLocationId))
        {
            throw new ActionForbiddenException(
                $"Action forbidden: Cannot assign reservation to a table in location '{tableLocationId}' because you are assigned to location '{waiter.LocationId}'.");
        }
    }

    private async Task<ReservationResponse> BuildReservationResponseWithDetailsAsync(Reservation reservation)
    {
        var location = await _locationService.GetLocationByTableIdAsync(reservation.TableId);
        var address = location?.Address ?? "Unknown address";
        Order? reservationOrder = await _orderService.GetOrderByReservationIdAsync(reservation.Id);

        return ReservationMapper.ToReservationResponse(reservation, address, reservationOrder);
    }
}
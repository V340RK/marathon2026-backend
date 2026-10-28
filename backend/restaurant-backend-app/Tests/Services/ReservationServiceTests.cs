using Amazon.CognitoIdentityProvider.Model;
using Amazon.Lambda.Core;
using AutoFixture.Xunit2;
using Moq;
using APIHandler.Constants;
using APIHandler.Entities;
using APIHandler.Models;
using APIHandler.Models.Reservations;
using APIHandler.Repositories.Auth;
using APIHandler.Repositories.Locations;
using APIHandler.Repositories.Reservations;
using APIHandler.Services;
using APIHandler.Services.Locations;
using APIHandler.Services.Orders;
using APIHandler.Services.Reservations;
using APIHandler.Services.Waiters;
using APIHandler.Models.WaiterList;
using APIHandler.Exceptions;
using APIHandler.Services.GroupReservations;

namespace APIHandler.Tests.Services;

public class ReservationServiceTests
{
    private readonly Mock<IReservationRepository> _mockReservationRepository = new();
    private readonly Mock<ILocationRepository> _mockLocationRepository = new();
    private readonly Mock<IUserRepository> _mockUserRepository = new();
    private readonly Mock<IWaiterService> _mockWaiterService = new();
    private readonly Mock<ILocationService> _mockLocationService = new();
    private readonly Mock<IOrderService> _mockOrderService = new();
    private readonly Mock<ILambdaLogger> _mockLogger = new();
    private readonly Mock<IGroupReservationService> _mockGroupReservationService = new();

    private readonly ReservationService _reservationService;

    public ReservationServiceTests()
    {
        _reservationService = new ReservationService(
            _mockReservationRepository.Object,
            _mockLocationRepository.Object,
            _mockUserRepository.Object,
            _mockWaiterService.Object,
            _mockLocationService.Object,
            _mockOrderService.Object,
            _mockGroupReservationService.Object
        );
    }

    [Theory, AutoData]
    public async Task CreateReservationAsync_ValidCustomerRequest_ShouldReturnReservationResponse(
        CreateReservationRequest request,
        string callerId,
        Location location,
        Table table,
        WaiterListItem waiter)
    {
        table.Id = request.TableId;
        table.LocationId = location.Id;
        waiter.LocationId = location.Id;
        request.StartTime = DateTime.UtcNow.AddHours(1);
        request.EndTime = request.StartTime.AddHours(1);
        request.GuestCount = 2;
        table.Capacity = 4;

        _mockLocationService
            .Setup(r => r.GetLocationByTableIdAsync(request.TableId))
            .ReturnsAsync(location);

        _mockWaiterService
            .Setup(r => r.GetLeastBusyWaiterAsync(location.Id))
            .ReturnsAsync(waiter);

        _mockWaiterService
            .Setup(r => r.FindWaiterByIdAsync(waiter.UserId))
            .ReturnsAsync(waiter);

        _mockReservationRepository
            .Setup(r => r.GetReservationsByTableIdAsync(request.TableId))
            .ReturnsAsync([]);

        _mockLocationRepository
            .Setup(r => r.GetTableByIdAsync(request.TableId))
            .ReturnsAsync(table);

        // Act
        var result = await _reservationService.CreateReservationAsync(request, callerId, isWaiter: false, _mockLogger.Object);

        _mockReservationRepository
            .Verify(r => r.SaveReservationAsync(It.Is<Reservation>(
                r => r.TableId == request.TableId
                && r.Status == ReservationStatus.Reserved
                && r.CreatedByWaiter == false
                && r.CustomerId == callerId)), Times.Once());
    }

    [Theory, AutoData]
    public async Task CreateReservationAsync_ValidWaiterRequest_ShouldReturnReservationResponse(
        CreateReservationRequest request,
        Location location,
        Table table,
        WaiterListItem waiter,
        AdminGetUserResponse customer,
        string customerId)
    {
        table.Id = request.TableId;
        table.LocationId = location.Id;
        waiter.LocationId = location.Id;
        request.StartTime = DateTime.UtcNow.AddHours(1);
        request.EndTime = request.StartTime.AddHours(1);
        request.GuestCount = 2;
        table.Capacity = 4;
        customer.UserAttributes.Add(new AttributeType()
        {
            Name = "sub",
            Value = customerId
        });

        _mockLocationService
            .Setup(r => r.GetLocationByTableIdAsync(request.TableId))
            .ReturnsAsync(location);

        _mockWaiterService
            .Setup(r => r.GetLeastBusyWaiterAsync(location.Id))
            .ReturnsAsync(waiter);

        _mockWaiterService
            .Setup(r => r.FindWaiterByIdAsync(waiter.UserId))
            .ReturnsAsync(waiter);

        _mockReservationRepository
            .Setup(r => r.GetReservationsByTableIdAsync(request.TableId))
            .ReturnsAsync([]);

        _mockLocationRepository
            .Setup(r => r.GetTableByIdAsync(request.TableId))
            .ReturnsAsync(table);

        _mockUserRepository
            .Setup(r => r.GetUserByEmailAsync(request.CustomerEmail))
            .ReturnsAsync(customer);

        // Act
        var result = await _reservationService.CreateReservationAsync(request, waiter.UserId, isWaiter: true, _mockLogger.Object);

        _mockReservationRepository
            .Verify(r => r.SaveReservationAsync(It.Is<Reservation>(
                r => r.TableId == request.TableId
                && r.Status == ReservationStatus.Reserved
                && r.CreatedByWaiter == true
                && r.CustomerId == customerId)), Times.Once());
    }

    [Theory, AutoData]
    public async Task CreateReservationAsync_WhenTwoOrMoreGuests_ShouldCreateGroupReservation(
        CreateReservationRequest request,
        string callerId,
        Location location,
        Table table,
        WaiterListItem waiter)
    {
        table.Id = request.TableId;
        table.LocationId = location.Id;
        waiter.LocationId = location.Id;
        request.StartTime = DateTime.UtcNow.AddHours(1);
        request.EndTime = request.StartTime.AddHours(1);
        request.GuestCount = 2;
        table.Capacity = 4;

        _mockLocationService
            .Setup(r => r.GetLocationByTableIdAsync(request.TableId))
            .ReturnsAsync(location);

        _mockWaiterService
            .Setup(r => r.GetLeastBusyWaiterAsync(location.Id))
            .ReturnsAsync(waiter);

        _mockWaiterService
            .Setup(r => r.FindWaiterByIdAsync(waiter.UserId))
            .ReturnsAsync(waiter);

        _mockReservationRepository
            .Setup(r => r.GetReservationsByTableIdAsync(request.TableId))
            .ReturnsAsync([]);

        _mockLocationRepository
            .Setup(r => r.GetTableByIdAsync(request.TableId))
            .ReturnsAsync(table);

        // Act
        var result = await _reservationService.CreateReservationAsync(request, callerId, isWaiter: false, _mockLogger.Object);

        _mockGroupReservationService.Verify(r => r.CreateGroupReservationAsync(It.IsAny<string>()));
    }

    [Theory, AutoData]
    public async Task CreateReservationAsync_WhenOneGuest_ShouldNotCreateGroupReservation(
        CreateReservationRequest request,
        string callerId,
        Location location,
        Table table,
        WaiterListItem waiter)
    {
        table.Id = request.TableId;
        table.LocationId = location.Id;
        waiter.LocationId = location.Id;
        request.StartTime = DateTime.UtcNow.AddHours(1);
        request.EndTime = request.StartTime.AddHours(1);
        request.GuestCount = 1;
        table.Capacity = 4;

        _mockLocationService
            .Setup(r => r.GetLocationByTableIdAsync(request.TableId))
            .ReturnsAsync(location);

        _mockWaiterService
            .Setup(r => r.GetLeastBusyWaiterAsync(location.Id))
            .ReturnsAsync(waiter);

        _mockWaiterService
            .Setup(r => r.FindWaiterByIdAsync(waiter.UserId))
            .ReturnsAsync(waiter);

        _mockReservationRepository
            .Setup(r => r.GetReservationsByTableIdAsync(request.TableId))
            .ReturnsAsync([]);

        _mockLocationRepository
            .Setup(r => r.GetTableByIdAsync(request.TableId))
            .ReturnsAsync(table);

        // Act
        var result = await _reservationService.CreateReservationAsync(request, callerId, isWaiter: false, _mockLogger.Object);

        _mockGroupReservationService.Verify(r => r.CreateGroupReservationAsync(It.IsAny<string>()), Times.Never);
    }

    [Theory, AutoData]
    public async Task CreateReservationAsync_InvalidTable_ThrowsException(CreateReservationRequest request, string callerId)
    {
        var result = await Assert.ThrowsAsync<ValidationException>(
            () => _reservationService.CreateReservationAsync(request, callerId, isWaiter: false, _mockLogger.Object));

        Assert.Contains("was not found", result.Message);
    }

    [Theory, AutoData]
    public async Task CreateReservationAsync_WaiterFromDifferentLocation_ThrowsAccessViolationException(
        CreateReservationRequest request,
        Location location,
        Table table,
        WaiterListItem waiter)
    {
        table.Id = request.TableId;
        table.LocationId = location.Id;
        request.StartTime = DateTime.UtcNow.AddHours(1);
        request.EndTime = request.StartTime.AddHours(1);
        request.GuestCount = 2;
        table.Capacity = 4;

        _mockLocationService
            .Setup(r => r.GetLocationByTableIdAsync(request.TableId))
            .ReturnsAsync(location);

        _mockWaiterService
            .Setup(r => r.GetLeastBusyWaiterAsync(location.Id))
            .ReturnsAsync(waiter);

        _mockWaiterService
            .Setup(r => r.FindWaiterByIdAsync(waiter.UserId))
            .ReturnsAsync(waiter);

        _mockReservationRepository
            .Setup(r => r.GetReservationsByTableIdAsync(request.TableId))
            .ReturnsAsync([]);

        _mockLocationRepository
            .Setup(r => r.GetTableByIdAsync(request.TableId))
            .ReturnsAsync(table);

        await Assert.ThrowsAsync<ActionForbiddenException>(
            () => _reservationService.CreateReservationAsync(request, waiter.UserId, isWaiter: true, _mockLogger.Object));
    }

    [Theory, AutoData]
    public async Task GetLocationIdForReservation_ValidId_Success(Reservation reservation, Table table)
    {
        reservation.TableId = table.Id;

        _mockReservationRepository
            .Setup(r => r.GetReservationByIdAsync(reservation.Id))
            .ReturnsAsync(reservation);

        _mockLocationRepository
            .Setup(r => r.GetTableByIdAsync(reservation.TableId))
            .ReturnsAsync(table);

        var result = await _reservationService.GetLocationIdForReservation(reservation.Id);

        Assert.Equal(table.LocationId, result);
    }

    [Theory, AutoData]
    public async Task UpdateReservationTableAsync_ValidCustomerRequest_ShouldReturnReservationResponse(
        Reservation reservation,
        Location location,
        Table newTable,
        Table oldTable,
        WaiterListItem waiter)
    {
        waiter.LocationId = location.Id;
        reservation.TableId = oldTable.Id;
        reservation.WaiterId = waiter.UserId;
        newTable.LocationId = location.Id;
        oldTable.LocationId = location.Id;
        newTable.Capacity = 4;
        reservation.GuestCount = 2;

        _mockLocationService
            .Setup(r => r.GetLocationByTableIdAsync(newTable.Id))
            .ReturnsAsync(location);

        _mockWaiterService
            .Setup(r => r.GetLeastBusyWaiterAsync(location.Id))
            .ReturnsAsync(waiter);

        _mockWaiterService
            .Setup(r => r.FindWaiterByIdAsync(waiter.UserId))
            .ReturnsAsync(waiter);

        _mockReservationRepository
            .Setup(r => r.GetReservationsByTableIdAsync(newTable.Id))
            .ReturnsAsync([]);

        _mockLocationRepository
            .Setup(r => r.GetTableByIdAsync(newTable.Id))
            .ReturnsAsync(newTable);

        _mockLocationRepository
            .Setup(r => r.GetTableByIdAsync(oldTable.Id))
            .ReturnsAsync(oldTable);

        _mockReservationRepository
           .Setup(r => r.GetReservationByIdAsync(reservation.Id))
           .ReturnsAsync(reservation);

        // Act
        var result = await _reservationService.UpdateReservationTableAsync(waiter.UserId, reservation.Id, newTable.Id, _mockLogger.Object);

        _mockReservationRepository
            .Verify(r => r.SaveReservationAsync(It.Is<Reservation>(r => r.TableId == newTable.Id)), Times.Once());
    }

    [Theory, AutoData]
    public async Task CreateReservationAsync_InvalidStartTime_LessThan15MinutesInAdvance_ThrowsValidationException(
        CreateReservationRequest request,
        string callerId,
        Location location,
        Table table,
        WaiterListItem waiter)
    {
        table.Id = request.TableId;
        table.LocationId = location.Id;
        waiter.LocationId = location.Id;
        request.StartTime = DateTime.UtcNow.AddMinutes(10);
        request.EndTime = request.StartTime.AddHours(1);
        request.GuestCount = 2;
        table.Capacity = 4;

        _mockLocationRepository
            .Setup(r => r.GetTableByIdAsync(request.TableId))
            .ReturnsAsync(table);

        _mockLocationService
            .Setup(r => r.GetLocationByTableIdAsync(table.Id))
            .ReturnsAsync(location);

        _mockWaiterService
            .Setup(r => r.GetLeastBusyWaiterAsync(location.Id))
            .ReturnsAsync(waiter);

        _mockWaiterService
            .Setup(r => r.FindWaiterByIdAsync(waiter.UserId))
            .ReturnsAsync(waiter);

        _mockReservationRepository
            .Setup(r => r.GetReservationsByTableIdAsync(request.TableId))
            .ReturnsAsync([]);

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _reservationService.CreateReservationAsync(request, callerId, isWaiter: false, _mockLogger.Object));

        Assert.Contains("15 minutes", exception.Message);
    }

    [Theory, AutoData]
    public async Task CreateReservationAsync_InvalidTimes_EndTimeNotAfterStartTime_ThrowsValidationException(
        CreateReservationRequest request,
        string callerId,
        Location location,
        Table table,
        WaiterListItem waiter)
    {
        table.Id = request.TableId;
        table.LocationId = location.Id;
        waiter.LocationId = location.Id;
        request.StartTime = DateTime.UtcNow.AddHours(2);
        request.EndTime = request.StartTime;
        request.GuestCount = 2;
        table.Capacity = 4;

        _mockLocationRepository
            .Setup(r => r.GetTableByIdAsync(request.TableId))
            .ReturnsAsync(table);

        _mockLocationService
            .Setup(r => r.GetLocationByTableIdAsync(table.Id))
            .ReturnsAsync(location);

        _mockWaiterService
            .Setup(r => r.GetLeastBusyWaiterAsync(location.Id))
            .ReturnsAsync(waiter);

        _mockWaiterService
            .Setup(r => r.FindWaiterByIdAsync(waiter.UserId))
            .ReturnsAsync(waiter);

        _mockReservationRepository
            .Setup(r => r.GetReservationsByTableIdAsync(request.TableId))
            .ReturnsAsync([]);

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _reservationService.CreateReservationAsync(request, callerId, isWaiter: false, _mockLogger.Object));

        Assert.Contains("must be after start time", exception.Message);
    }

    [Theory, AutoData]
    public async Task CreateReservationAsync_GuestCountExceedsCapacity_ThrowsValidationException(
        CreateReservationRequest request,
        string callerId,
        Location location,
        Table table,
        WaiterListItem waiter)
    {
        table.Id = request.TableId;
        table.LocationId = location.Id;
        waiter.LocationId = location.Id;
        request.StartTime = DateTime.UtcNow.AddHours(1);
        request.EndTime = request.StartTime.AddHours(1);
        request.GuestCount = 10;
        table.Capacity = 4;

        _mockLocationRepository
            .Setup(r => r.GetTableByIdAsync(request.TableId))
            .ReturnsAsync(table);

        _mockLocationService
            .Setup(r => r.GetLocationByTableIdAsync(table.Id))
            .ReturnsAsync(location);

        _mockWaiterService
            .Setup(r => r.GetLeastBusyWaiterAsync(location.Id))
            .ReturnsAsync(waiter);

        _mockWaiterService
            .Setup(r => r.FindWaiterByIdAsync(waiter.UserId))
            .ReturnsAsync(waiter);

        _mockReservationRepository
            .Setup(r => r.GetReservationsByTableIdAsync(request.TableId))
            .ReturnsAsync([]);

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _reservationService.CreateReservationAsync(request, callerId, isWaiter: false, _mockLogger.Object));

        Assert.Contains("exceeds table capacity", exception.Message);
    }

    [Theory, AutoData]
    public async Task CreateReservationAsync_ReservationOverlapsWithExisting_ThrowsValidationException(
        CreateReservationRequest request,
        string callerId,
        Location location,
        Table table,
        WaiterListItem waiter,
        Reservation existingReservation)
    {
        table.Id = request.TableId;
        table.LocationId = location.Id;
        waiter.LocationId = location.Id;
        request.StartTime = DateTime.UtcNow.AddHours(2);
        request.EndTime = request.StartTime.AddHours(1);
        request.GuestCount = 2;
        table.Capacity = 4;

        existingReservation.TableId = table.Id;
        existingReservation.StartTime = DateTime.UtcNow.AddHours(1);
        existingReservation.EndTime = existingReservation.StartTime.AddHours(2);

        _mockLocationService
            .Setup(r => r.GetLocationByTableIdAsync(request.TableId))
            .ReturnsAsync(location);

        _mockWaiterService
            .Setup(r => r.GetLeastBusyWaiterAsync(location.Id))
            .ReturnsAsync(waiter);

        _mockWaiterService
            .Setup(r => r.FindWaiterByIdAsync(waiter.UserId))
            .ReturnsAsync(waiter);

        _mockReservationRepository
            .Setup(r => r.GetReservationsByTableIdAsync(request.TableId))
            .ReturnsAsync([existingReservation]);

        _mockLocationRepository
            .Setup(r => r.GetTableByIdAsync(request.TableId))
            .ReturnsAsync(table);

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _reservationService.CreateReservationAsync(request, callerId, isWaiter: false, _mockLogger.Object));

        Assert.Contains("overlaps", exception.Message);
    }

    [Theory, AutoData]
    public async Task GetLocationIdForReservation_ReservationNotFound_ThrowsReservationNotFoundException(string reservationId)
    {
        _mockReservationRepository
            .Setup(r => r.GetReservationByIdAsync(reservationId))
            .ReturnsAsync((Reservation?)null);

        var exception = await Assert.ThrowsAsync<ReservationNotFoundException>(
            () => _reservationService.GetLocationIdForReservation(reservationId));

        Assert.Contains("not found", exception.Message);
    }

    [Theory, AutoData]
    public async Task GetLocationIdForReservation_TableNotFound_ValidationException(Reservation reservation)
    {
        _mockReservationRepository
            .Setup(r => r.GetReservationByIdAsync(reservation.Id))
            .ReturnsAsync(reservation);

        _mockLocationRepository
            .Setup(r => r.GetTableByIdAsync(reservation.TableId))
            .ReturnsAsync((Table?)null);

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _reservationService.GetLocationIdForReservation(reservation.Id));

        Assert.Contains("Table with ID", exception.Message);
    }

    [Theory, AutoData]
    public async Task UpdateReservationTableAsync_ReservationNotFound_ThrowsReservationNotFoundException(
        string callerId,
        string reservationId,
        string newTableId)
    {
        _mockReservationRepository
            .Setup(r => r.GetReservationByIdAsync(reservationId))
            .ReturnsAsync((Reservation?)null);

        var exception = await Assert.ThrowsAsync<ReservationNotFoundException>(
            () => _reservationService.UpdateReservationTableAsync(callerId, reservationId, newTableId, _mockLogger.Object));

        Assert.Contains("Reservation not found", exception.Message);
    }

    [Theory, AutoData]
    public async Task UpdateReservationTableAsync_NewTableNotFound_ThrowsValidationException(
        Reservation reservation,
        WaiterListItem waiter,
        string newTableId)
    {
        reservation.WaiterId = waiter.UserId;

        _mockReservationRepository
            .Setup(r => r.GetReservationByIdAsync(reservation.Id))
            .ReturnsAsync(reservation);

        _mockWaiterService
            .Setup(r => r.FindWaiterByIdAsync(waiter.UserId))
            .ReturnsAsync(waiter);

        _mockLocationRepository
            .Setup(r => r.GetTableByIdAsync(newTableId))
            .ReturnsAsync((Table?)null);

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _reservationService.UpdateReservationTableAsync(waiter.UserId, reservation.Id, newTableId, _mockLogger.Object));

        Assert.Contains("not found", exception.Message);
    }

    [Theory, AutoData]
    public async Task UpdateReservationTableAsync_WaiterDoesNotOwnReservation_ThrowsActionForbiddenException(
        Reservation reservation,
        WaiterListItem waiter,
        string differentWaiterId,
        string newTableId)
    {
        reservation.WaiterId = differentWaiterId;

        _mockReservationRepository
            .Setup(r => r.GetReservationByIdAsync(reservation.Id))
            .ReturnsAsync(reservation);

        var exception = await Assert.ThrowsAsync<ActionForbiddenException>(
            () => _reservationService.UpdateReservationTableAsync(waiter.UserId, reservation.Id, newTableId, _mockLogger.Object));

        Assert.Contains("not the assigned waiter", exception.Message);
    }

    [Theory, AutoData]
    public async Task UpdateReservationTableAsync_ReservationHasNoAssignedWaiter_ThrowsValidationException(
        Reservation reservation,
        string callerId,
        string newTableId)
    {
        reservation.WaiterId = null;

        _mockReservationRepository
            .Setup(r => r.GetReservationByIdAsync(reservation.Id))
            .ReturnsAsync(reservation);

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _reservationService.UpdateReservationTableAsync(callerId, reservation.Id, newTableId, _mockLogger.Object));

        Assert.Contains("no assigned waiter", exception.Message);
    }

    [Theory, AutoData]
    public async Task UpdateReservationTableAsync_WaiterNotAssignedToTableLocation_ThrowsActionForbiddenException(
        Reservation reservation,
        Location location,
        Table newTable,
        Table oldTable,
        WaiterListItem waiter)
    {
        waiter.LocationId = "different-location-id";
        reservation.TableId = oldTable.Id;
        reservation.WaiterId = waiter.UserId;
        newTable.LocationId = location.Id;
        oldTable.LocationId = location.Id;

        _mockReservationRepository
            .Setup(r => r.GetReservationByIdAsync(reservation.Id))
            .ReturnsAsync(reservation);

        _mockWaiterService
            .Setup(r => r.FindWaiterByIdAsync(waiter.UserId))
            .ReturnsAsync(waiter);

        _mockLocationRepository
            .Setup(r => r.GetTableByIdAsync(oldTable.Id))
            .ReturnsAsync(oldTable);

        var exception = await Assert.ThrowsAsync<ActionForbiddenException>(
            () => _reservationService.UpdateReservationTableAsync(waiter.UserId, reservation.Id, newTable.Id, _mockLogger.Object));

        Assert.Contains("assigned to location", exception.Message);
    }

    [Theory, AutoData]
    public async Task UpdateReservationTableAsync_GuestCountExceedsNewTableCapacity_ThrowsValidationException(
        Reservation reservation,
        Location location,
        Table newTable,
        Table oldTable,
        WaiterListItem waiter)
    {
        waiter.LocationId = location.Id;
        reservation.TableId = oldTable.Id;
        reservation.WaiterId = waiter.UserId;
        reservation.GuestCount = 10;
        newTable.LocationId = location.Id;
        newTable.Capacity = 4;
        oldTable.LocationId = location.Id;

        _mockReservationRepository
            .Setup(r => r.GetReservationByIdAsync(reservation.Id))
            .ReturnsAsync(reservation);

        _mockWaiterService
            .Setup(r => r.FindWaiterByIdAsync(waiter.UserId))
            .ReturnsAsync(waiter);

        _mockLocationRepository
            .Setup(r => r.GetTableByIdAsync(oldTable.Id))
            .ReturnsAsync(oldTable);

        _mockLocationRepository
            .Setup(r => r.GetTableByIdAsync(newTable.Id))
            .ReturnsAsync(newTable);

        _mockReservationRepository
            .Setup(r => r.GetReservationsByTableIdAsync(newTable.Id))
            .ReturnsAsync([]);

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _reservationService.UpdateReservationTableAsync(waiter.UserId, reservation.Id, newTable.Id, _mockLogger.Object));

        Assert.Contains("exceeds table capacity", exception.Message);
    }

    [Theory, AutoData]
    public async Task UpdateReservationTableAsync_OverlapWithExistingReservationOnNewTable_ThrowsValidationException(
        Reservation reservation,
        Reservation existingReservation,
        Location location,
        Table newTable,
        Table oldTable,
        WaiterListItem waiter)
    {
        waiter.LocationId = location.Id;
        reservation.TableId = oldTable.Id;
        reservation.WaiterId = waiter.UserId;
        reservation.GuestCount = 2;
        reservation.StartTime = DateTime.UtcNow.AddHours(2);
        reservation.EndTime = reservation.StartTime.AddHours(1);
        newTable.LocationId = location.Id;
        newTable.Capacity = 4;
        oldTable.LocationId = location.Id;

        existingReservation.TableId = newTable.Id;
        existingReservation.StartTime = DateTime.UtcNow.AddHours(1);
        existingReservation.EndTime = existingReservation.StartTime.AddHours(2);

        _mockReservationRepository
            .Setup(r => r.GetReservationByIdAsync(reservation.Id))
            .ReturnsAsync(reservation);

        _mockWaiterService
            .Setup(r => r.FindWaiterByIdAsync(waiter.UserId))
            .ReturnsAsync(waiter);

        _mockLocationRepository
            .Setup(r => r.GetTableByIdAsync(newTable.Id))
            .ReturnsAsync(newTable);

        _mockLocationRepository
            .Setup(r => r.GetTableByIdAsync(oldTable.Id))
            .ReturnsAsync(oldTable);

        _mockReservationRepository
            .Setup(r => r.GetReservationsByTableIdAsync(newTable.Id))
            .ReturnsAsync([existingReservation]);

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _reservationService.UpdateReservationTableAsync(waiter.UserId, reservation.Id, newTable.Id, _mockLogger.Object));

        Assert.Contains("overlaps", exception.Message);
    }

    [Theory]
    [AutoData]
    public async Task UpdateReservationAsync_WhenTwoOrMoreGuestCount_ShouldCreateGroupReservation(string userId, Reservation reservation)
    {
        reservation.CustomerId = userId;
        reservation.GuestCount = 2;
        reservation.Status = ReservationStatus.Reserved;

        var result = await _reservationService.UpdateReservationAsync(reservation);

        _mockGroupReservationService.Verify(r => r.CreateGroupReservationIfDoesNotExistAsync(userId, reservation.Id));
    }

    [Theory]
    [AutoData]
    public async Task UpdateReservationAsync_WhenOneGuest_ShouldNotCreateGroupReservation(string userId, Reservation reservation)
    {
        reservation.CustomerId = userId;
        reservation.GuestCount = 1;
        reservation.Status = ReservationStatus.Reserved;

        var result = await _reservationService.UpdateReservationAsync(reservation);

        _mockGroupReservationService.Verify(r => r.CreateGroupReservationIfDoesNotExistAsync(userId, reservation.Id), Times.Never);
    }

    [Theory]
    [InlineAutoData(ReservationStatus.InProgress)]
    [InlineAutoData(ReservationStatus.Cancelled)]
    [InlineAutoData(ReservationStatus.Completed)]
    public async Task UpdateGroupReservation_StatusNotReservated_ShouldCloseGroupReservation(ReservationStatus status, Reservation reservation)
    {
        reservation.GuestCount = 3;
        reservation.Status = status;

        var result = await _reservationService.UpdateReservationAsync(reservation);

        _mockGroupReservationService.Verify(r => r.CloseGroupReservationAsync(reservation.Id), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task UpdateGroupReservation_OneGuestAndStatusNotReservated_ShouldCloseGroupReservation(Reservation reservation)
    {
        reservation.GuestCount = 1;
        reservation.Status = ReservationStatus.Completed;

        var result = await _reservationService.UpdateReservationAsync(reservation);

        _mockGroupReservationService.Verify(r => r.CloseGroupReservationAsync(reservation.Id), Times.Once());
    }
}

using APIHandler.Entities;
using APIHandler.Models.Users;
using APIHandler.Models.WaiterList;
using APIHandler.Repositories.Auth;
using APIHandler.Repositories.Locations;
using APIHandler.Repositories.Waiters;
using APIHandler.Services.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APIHandler.Exceptions;

namespace APIHandler.Services.Waiters;

public class WaiterService : IWaiterService
{
    private readonly IWaiterRepository _waiterRepository;
    private readonly ILocationRepository _locationRepository;
    private readonly IUserService _userService;
    private readonly IUserRepository _userRepository;

    public WaiterService(
        IWaiterRepository waiterRepository,
        ILocationRepository locationRepository,
        IUserService userService,
        IUserRepository userRepository)
    {
        _waiterRepository = waiterRepository ?? throw new ArgumentNullException(nameof(waiterRepository));
        _locationRepository = locationRepository ?? throw new ArgumentNullException(nameof(locationRepository));
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public async Task AddWaiterToListAsync(UpdateWaiterListRequest request)
    {
        var location = await GetLocationByIdAsync(request.LocationId);

        var user = await _userRepository.GetUserByEmailAsync(request.Email);
        var userId = user.UserAttributes.FirstOrDefault(attr => attr.Name == "sub")?.Value;

        var newItem = new WaiterListItem
        {
            UserId = userId ?? throw new InvalidOperationException($"User ID not found in Cognito user attributes for email '{request.Email}'."),
            LocationId = request.LocationId,
            CreatedAt = DateTime.UtcNow,
        };

        await _waiterRepository.SaveWaiterAsync(newItem);

        await _userRepository.EnsureGroupExistsAsync(Roles.Waiter.ToString());

        await _userRepository.AddUserToGroupAsync(request.Email, Roles.Waiter.ToString());
    }

    public async Task RemoveWaiterFromListAsync(string email)
    {
        var emailTrimmed = email.Trim().ToLower();

        if (string.IsNullOrWhiteSpace(emailTrimmed))
        {
            return;
        }

        var user = await _userRepository.GetUserByEmailAsync(emailTrimmed);
        var userId = user.UserAttributes.FirstOrDefault(attr => attr.Name == "sub")?.Value;

        if (userId == null)
        {
            return;
        }

        var existingWaiter = await _waiterRepository.GetWaiterByIdAsync(userId);

        if (existingWaiter == null)
        {
            return;
        }

        await _waiterRepository.DeleteWaiterAsync(existingWaiter);

        await _userRepository.RemoveUserFromGroupAsync(emailTrimmed, Roles.Waiter.ToString());
    }

    public async Task<bool> IsWaiterInListAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        string? userId;

        try
        {
            userId = await _userService.GetUserIdByEmailAsync(email.Trim().ToLower());
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error retrieving user ID for email {email}: {ex.Message}", ex);
        }

        if (userId is null)
            return false;

        var existing = await _waiterRepository.GetWaiterByIdAsync(userId);
        return existing is not null;
    }

    public async Task<WaiterListItem?> FindWaiterByReservationIdAsync(string reservationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reservationId);

        var waiters = await _waiterRepository.GetAllWaitersAsync();

        return waiters.FirstOrDefault(w => w.Reservations != null && w.Reservations.Contains(reservationId));
    }

    public async Task<WaiterListItem> FindWaiterByIdAsync(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var waiter = await _waiterRepository.GetWaiterByIdAsync(userId);

        if (waiter == null)
        {
            throw new ValidationException($"Waiter with userId '{userId}' not found.");
        }

        return waiter;
    }

    public async Task<WaiterListItem> GetLeastBusyWaiterAsync(string locationId)
    {
        var waiters = await _waiterRepository.GetWaitersByLocationIdAsync(locationId);
        if (waiters.Count == 0)
        {
            throw new ValidationException($"No waiters found for location ID {locationId}.");
        }

        return waiters
            .OrderBy(w => w.Reservations?.Count ?? 0)
            .First();
    }

    public async Task UpdateWaiterReservationsAsync(string userId, IEnumerable<string> reservationIds)
    {
        var waiter = await _waiterRepository.GetWaiterByIdAsync(userId)
            ?? throw new InvalidOperationException($"Waiter with User ID '{userId}' does not exist.");
        waiter.Reservations = reservationIds.ToList();

        await _waiterRepository.SaveWaiterAsync(waiter);
    }

    public async Task<WaiterListItem?> GetWaiterByReservationIdAsync(string reservationId)
    {
        var waiters = await _waiterRepository.GetAllWaitersAsync();
        return waiters.SingleOrDefault(w => w.Reservations != null && w.Reservations.Contains(reservationId));
    }

    private async Task<Location> GetLocationByIdAsync(string locationId) =>
        await _locationRepository.GetLocationByIdAsync(locationId)
        ?? throw new ArgumentException($"Location with ID {locationId} does not exist.");

    public async Task<string?> GetWaiterLocationIdAsync(string waiterId)
    {
        if (string.IsNullOrWhiteSpace(waiterId))
        {
            throw new ArgumentException("Waiter ID cannot be null or empty.", nameof(waiterId));
        }

        var waiter = await _waiterRepository.GetWaiterByIdAsync(waiterId);

        return waiter?.LocationId;
    }

    public async Task<List<WaiterListItem>> GetWaitersByLocationIdAsync(string locationId)
    {
        return await _waiterRepository.GetWaitersByLocationIdAsync(locationId);
    }

    public async Task RemoveWaiterByIdAsync(string userId)
    {
        var existingWaiter = await _waiterRepository.GetWaiterByIdAsync(userId);
        if (existingWaiter == null) return;

        var email = await _userService.GetUserEmail(userId);

        await _waiterRepository.DeleteWaiterAsync(existingWaiter);

        if (!string.IsNullOrWhiteSpace(email))
        {
            await _userRepository.RemoveUserFromGroupAsync(email, Roles.Waiter.ToString());
        }
    }
}
using Amazon.CognitoIdentityProvider.Model;
using APIHandler.Constants;
using APIHandler.Constants.Dishes;
using APIHandler.Constants.Orders;
using APIHandler.Entities;
using APIHandler.Exceptions;
using APIHandler.Mappers;
using APIHandler.Models.GuestSessions;
using APIHandler.Models.Orders;
using APIHandler.Repositories.Auth;
using APIHandler.Repositories.Dishes;
using APIHandler.Repositories.GroupReservations;
using APIHandler.Repositories.GuestSessions;
using APIHandler.Repositories.Orders;
using APIHandler.Repositories.Reservations;
using APIHandler.Repositories.Tables;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using APIHandler.Services.Notifications;

namespace APIHandler.Services.GuestSessions;

public class GuestSessionService(
    IGroupReservationRepository groupReservationRepository,
    IGuestSessionRepository guestSessionRepository,
    IDishRepository dishRepository,
    IReservationRepository reservationRepository,
    IUserRepository userRepository,
    ITableRepository tableRepository,
    IOrderRepository orderRepository) : IGuestSessionService
{
    public async Task<bool> IsAuthorizedAsync(string guestSessionToken, string guestSessionId)
    {
        string hash = Convert.ToBase64String(SHA256.HashData(Convert.FromBase64String(guestSessionToken)));
        GuestSession? guestSession = await guestSessionRepository.GetByTokenHash(hash);
        if (guestSession is null)
            return false;

        return guestSession.Id == guestSessionId;
    }

    public async Task<CreatGuestSessionResponse> CreateGuestSessionAsync(CreateGuestSessionRequest request)
    {
        GroupReservation group = await GetGroupReservationAndValidateAsync(request.GroupReservationId);

        Task<Reservation?> reservationTask = reservationRepository.GetReservationByIdAsync(group.ReservationId);
        Task<int> countTask = guestSessionRepository.CountByGroupReservationId(group.GroupReservationId);

        Reservation reservation = await reservationTask ?? throw new GroupReservationExpiredException($"Associated reservation for group '{request.GroupReservationId}' not found.");

        if (reservation.Status != ReservationStatus.Reserved)
        {
            throw new GuestSessionExpiredException($"This pre-order session has expired or is no longer active. Current reservation status: '{reservation.Status}'.");
        }

        int joinedGuestsCount = await countTask;

        if (joinedGuestsCount + 1 >= reservation.GuestCount)
            throw new GuestLimitExceededException($"Guest limit exceeded. Maximum capacity is {reservation.GuestCount}, but {joinedGuestsCount} guests have already joined.");

        byte[] tokenBytes = RandomNumberGenerator.GetBytes(32);

        var session = new GuestSession()
        {
            Id = Guid.NewGuid().ToString(),
            GuestName = request.GuestName,
            GroupReservationId = group.GroupReservationId,
            SessionTokenHash = Convert.ToBase64String(SHA256.HashData(tokenBytes)),
            Dishes = [],
        };

        await guestSessionRepository.SaveAsync(session);

        return new CreatGuestSessionResponse(session.Id, Convert.ToBase64String(tokenBytes));
    }

    public async Task<GuestMenuResponse> GetGuestMenuAsync(string guestSessionId)
    {
        var session = await guestSessionRepository.GetByGuestSessionId(guestSessionId)
            ?? throw new ValidationException($"Guest session '{guestSessionId}' not found.");

        await ValidateGroupReservationId(session.GroupReservationId);

        string locationId = await GetLocationIdForGroupAsync(session.GroupReservationId);
        var dishes = await dishRepository.GetDishesByLocationIdAsync(locationId);

        var dishesMapped = dishes
            .GroupBy(d => d.DishType)
            .ToDictionary(k => k.Key, GuestMenuDishMapper.Map);

        return new GuestMenuResponse(dishesMapped);
    }

    public async Task AddDishAsync(string guestSessionId, string dishId, int quantity)
    {
        if (quantity <= 0)
            throw new ValidationException($"Quantity ({quantity}) must be greater than zero.");

        GuestSession session = await GetGuestSessionAsync(guestSessionId);

        string locationId = await GetLocationIdForGroupAsync(session.GroupReservationId);
        var locationDishes = await dishRepository.GetDishesByLocationIdAsync(locationId);

        var dish = locationDishes.FirstOrDefault(d => d.Id == dishId)
            ?? throw new ValidationException($"Dish '{dishId}' does not exist or is not available at this location.");

        if (dish.State != DishState.Available)
            throw new DishIsNotAvailableException($"Dish '{dishId}' is currently unavailable. State: '{dish.State}'.");

        session.Dishes ??= [];

        var existingDish = session.Dishes.FirstOrDefault(d => d.DishId == dishId);

        if (existingDish != null)
        {
            existingDish.Quantity += quantity;
        }
        else
        {
            session.Dishes.Add(new OrderItem
            {
                DishId = dishId,
                Quantity = quantity,
                State = OrderItemState.Pending
            });
        }

        await guestSessionRepository.SaveAsync(session);
    }

    public async Task RemoveDishAsync(string guestSessionId, string dishId, int quantity)
    {
        if (quantity <= 0)
            throw new ValidationException("Quantity must be greater than zero.");

        GuestSession session = await GetGuestSessionAsync(guestSessionId);

        if (session.Dishes == null || session.Dishes.Count == 0)
            throw new DishNotFoundException($"Dish {dishId} not found in guest session.");

        var existingDish = session.Dishes.FirstOrDefault(d => d.DishId == dishId)
            ?? throw new DishNotFoundException($"Dish {dishId} not found in guest session.");

        existingDish.Quantity -= quantity;

        if (existingDish.Quantity <= 0)
        {
            session.Dishes.Remove(existingDish);
        }

        await guestSessionRepository.SaveAsync(session);
    }

    public async Task<GroupReservationStatusResponse> GetGroupReservationStatusAsync(string groupReservationId)
    {
        var group = await groupReservationRepository.GetGroupReservationByIdAsync(groupReservationId)
            ?? throw new GroupReservationNotFoundException();

        var reservation = await reservationRepository.GetReservationByIdAsync(group.ReservationId)
            ?? throw new InvalidOperationException("Associated reservation not found.");

        int currentParticipants = await GetCurrentParticipantCountAsync(groupReservationId);

        return new GroupReservationStatusResponse
        {
            CurrentParticipants = currentParticipants,
            MaxCapacity = reservation.GuestCount,
            Status = group.Status
        };
    }

    
    public async Task<GetAllSessionsResponse> DeleteGuestSessionAsync(string userId, string groupReservationId, string guestSessionId)
    {
        await ValidateReservationOwnerAsync(groupReservationId, userId);

        var session = await guestSessionRepository.GetByGuestSessionId(guestSessionId)
            ?? throw new GuestSessionNotFoundException($"Guest session with id {guestSessionId} not found.");

        if (session.GroupReservationId != groupReservationId)
        {
            throw new ValidationException("Guest session does not belong to this group reservation.");
        }

        await guestSessionRepository.DeleteAsync(session);

        return await BuildSessionsSnapshotAsync(groupReservationId);
    }

    public async Task<GetAllSessionsResponse> GetAllSessionsAsync(string userId, string groupReservationId)
    {
        await ValidateReservationOwnerAsync(groupReservationId, userId);

        return await BuildSessionsSnapshotAsync(groupReservationId);
    }

    public async Task<OwnerGuestSessionResponse> GetOwnerGuestSessionIdAsync(string userId, string reservationId)
    {
        var reservation = await reservationRepository.GetReservationByIdAsync(reservationId)
            ?? throw new ReservationNotFoundException($"Reservation {reservationId} not found.");

        if (reservation.CustomerId != userId)
            throw new ActionForbiddenException("Only the reservation owner can perform this action.");

        var group = await groupReservationRepository.GetByReservationIdAsync(reservationId)
            ?? throw new GroupReservationNotFoundException();

        var sessions = await guestSessionRepository.GetAllByGroupReservationIdAsync(group.GroupReservationId);

        // Owner session is created lazily: nothing else in the flow creates it yet
        var ownerSession = sessions.FirstOrDefault(s => s.IsOwnerSession && s.OwnerUserId == userId)
            ?? await CreateGroupOwnerSessionAsync(userId, group.GroupReservationId);

        return new OwnerGuestSessionResponse(ownerSession.Id);
    }

    public async Task<IReadOnlyList<SubmittedDishResponse>> SubmitGroupPreOrderAsync(string userId, string groupReservationId, string reservationId)
    {
        var group = await groupReservationRepository.GetGroupReservationByIdAsync(groupReservationId)
            ?? throw new GroupReservationNotFoundException();

        if (group.ReservationId != reservationId)
            throw new ValidationException("Reservation does not belong to this group reservation.");

        var reservation = await reservationRepository.GetReservationByIdAsync(reservationId)
            ?? throw new ReservationNotFoundException($"Reservation {reservationId} not found.");

        if (reservation.CustomerId != userId)
            throw new ActionForbiddenException("Only the reservation owner can perform this action.");

        if (group.Status == GroupReservationStatus.Submitted)
            throw new GroupReservationSubmittedException();

        var submittedSessions = (await guestSessionRepository.GetAllByGroupReservationIdAsync(groupReservationId))
            .Where(s => s.IsSubmitted)
            .ToList();

        if (submittedSessions.Count == 0)
            throw new ValidationException("There are no submitted guest sessions.");

        var mergedDishes = submittedSessions
            .SelectMany(s => s.Dishes ?? [])
            .GroupBy(d => d.DishId)
            .Select(g => new SubmittedDishResponse(g.Key, g.Sum(d => d.Quantity)))
            .Where(d => d.Quantity > 0)
            .ToList();

        if (mergedDishes.Count == 0)
            throw new ValidationException("Submitted guest sessions contain no dishes.");

        var order = await orderRepository.GetOrderByReservationIdAsync(reservationId)
            ?? throw new ValidationException($"Order for reservation {reservationId} not found.");

        order.Dishes ??= [];

        foreach (var dish in mergedDishes)
        {
            var existing = order.Dishes.FirstOrDefault(d => d.DishId == dish.DishId);
            if (existing != null)
            {
                existing.Quantity += dish.Quantity;
            }
            else
            {
                order.Dishes.Add(new OrderItem
                {
                    DishId = dish.DishId,
                    Quantity = dish.Quantity,
                    State = OrderItemState.Pending
                });
            }
        }

        order.Status = OrderStatus.SubmittedByCustomer;
        await orderRepository.SaveOrderAsync(order);

        group.Status = GroupReservationStatus.Submitted;
        await groupReservationRepository.SaveGroupReservationAsync(group);

        return mergedDishes;
    }

    public async Task<GetAllSessionsResponse> SetGuestBillTypeAsync(string userId, string groupReservationId, string guestSessionId, BillType billType)
    {
        await ValidateReservationOwnerAsync(groupReservationId, userId);
        await ValidateGroupReservationId(groupReservationId);

        var session = await guestSessionRepository.GetByGuestSessionId(guestSessionId)
            ?? throw new GuestSessionNotFoundException($"Guest session with id {guestSessionId} not found.");

        if (session.GroupReservationId != groupReservationId)
            throw new ValidationException("Guest session does not belong to this group reservation.");

        if (session.IsOwnerSession)
            throw new ValidationException("The owner's own dishes are always on the shared bill.");

        if (session.BillType != billType)
        {
            session.BillType = billType;
            await guestSessionRepository.SaveAsync(session);
        }

        return await BuildSessionsSnapshotAsync(groupReservationId);
    }

    // A guest may only take their own order off the owner's bill, never put it back on
    public async Task PayOwnOrderAsync(string guestSessionId)
    {
        var session = await guestSessionRepository.GetByGuestSessionId(guestSessionId)
            ?? throw new GuestSessionNotFoundException($"Guest session {guestSessionId} not found.");

        await ValidateGroupReservationId(session.GroupReservationId);

        if (session.IsOwnerSession)
            throw new ValidationException("The owner's own dishes are always on the shared bill.");

        if (session.BillType == BillType.Individual)
            return;

        session.BillType = BillType.Individual;
        await guestSessionRepository.SaveAsync(session);
    }

    private static BillType GetBillType(GuestSession session) =>
        session.IsOwnerSession ? BillType.Shared : session.BillType;

    private async Task<GetAllSessionsResponse> BuildSessionsSnapshotAsync(string groupReservationId)
    {
        var sessions = await guestSessionRepository.GetAllByGroupReservationIdAsync(groupReservationId);

        var dishIds = sessions
            .SelectMany(s => s.Dishes ?? [])
            .Select(s => s.DishId)
            .Distinct()
            .ToArray();

        var allDishes = dishIds.Length == 0 ? Enumerable.Empty<Dish>() : await dishRepository.GetDishesByIdsAsync(dishIds);
        var dishesById = allDishes.ToDictionary(d => d.Id);

        var participants = new List<Participant>();

        foreach (var guestSession in sessions)
        {
            var items = (guestSession.Dishes ?? [])
                .Where(d => dishesById.ContainsKey(d.DishId))
                .Select(d => new OrderItemResponse
                {
                    Dish = DishMapper.ToDishResponse(dishesById[d.DishId]),
                    Quantity = d.Quantity
                }).ToArray();

            decimal total = items.Sum(i => i.Quantity * i.Dish.Price);
            var type = guestSession.IsOwnerSession ? ParticipantType.Owner : ParticipantType.Guest;
            participants.Add(new Participant(guestSession.Id, type, guestSession.GuestName, items, total, guestSession.IsSubmitted, GetBillType(guestSession)));
        }

        return new GetAllSessionsResponse(
            participants,
            participants.Sum(p => p.Total),
            participants.Where(p => p.BillType == BillType.Shared).Sum(p => p.Total),
            participants.Where(p => p.BillType == BillType.Individual).Sum(p => p.Total));
    }
    public async Task SubmitCartAsync(string guestSessionId)
    {
        GuestSession guestSession = await GetGuestSessionAsync(guestSessionId);

        if (!guestSession.Dishes.Any())
            throw new CartIsEmptyException();

        await ValidateDishesAsync(guestSession.Dishes);

        guestSession.IsSubmitted = true;
        await guestSessionRepository.SaveAsync(guestSession);
    }

    private async Task<GroupReservation> GetGroupReservationAndValidateAsync(string groupReservationId)
    {
        GroupReservation? group = await groupReservationRepository.GetGroupReservationByIdAsync(groupReservationId);
        ValidateGroupReservation(group);
        return group;
    }

    private async Task<int> GetCurrentParticipantCountAsync(string groupReservationId)
    {
        var sessions = await guestSessionRepository.GetAllByGroupReservationIdAsync(groupReservationId);

        // Owner is always 1 active participant. 
        // If sessions is null/empty, it safely evaluates to 0 joined guests + 1 owner = 1.
        return (sessions?.Count ?? 0) + 1;
    }

    private async Task ValidateGroupReservationId(string groupReservationId) => await GetGroupReservationAndValidateAsync(groupReservationId);

    private static void ValidateGroupReservation([NotNull] GroupReservation? group)
    {
        if (group is null)
            throw new GroupReservationNotFoundException();

        if (group.Status == GroupReservationStatus.Closed)
            throw new GroupReservationExpiredException();

        if (group.Status == GroupReservationStatus.Submitted)
            throw new GroupReservationSubmittedException();
    }

    private async Task<GuestSession> GetGuestSessionAsync(string sessionId)
    {
        var session = await guestSessionRepository.GetByGuestSessionId(sessionId)
            ?? throw new GuestSessionNotFoundException($"Guest session {sessionId} not found.");

        if (session.IsSubmitted)
            throw new GuestSessionSubmittedException();

        await ValidateGroupReservationId(session.GroupReservationId);

        return session;
    }

    private async Task ValidateDishesAsync(IEnumerable<OrderItem> orders)
    {
        IEnumerable<string> dishIds = orders.Select(o => o.DishId).ToArray();
        IEnumerable<Dish> dishes = await dishRepository.GetDishesByIdsAsync(dishIds);

        if (dishes.FirstOrDefault(d => d.State != DishState.Available) is Dish dish)
            throw new DishIsNotAvailableException($"The dish '{dish.Name}' ({dish.Id}) is not available.");
    }

    private async Task<string> GetLocationIdForGroupAsync(string groupReservationId)
    {
        var group = await groupReservationRepository.GetGroupReservationByIdAsync(groupReservationId)
            ?? throw new GroupReservationNotFoundException();

        var reservation = await reservationRepository.GetReservationByIdAsync(group.ReservationId)
            ?? throw new ReservationNotFoundException("Parent reservation not found.");

        var table = await tableRepository.GetTableAsync(reservation.TableId)
            ?? throw new ValidationException("Associated table not found.");

        return table.LocationId;
    }

    
    private async Task ValidateReservationOwnerAsync(string groupReservationId, string userId)
    {
        var group = await groupReservationRepository.GetGroupReservationByIdAsync(groupReservationId)
            ?? throw new GroupReservationNotFoundException();

        var reservation = await reservationRepository.GetReservationByIdAsync(group.ReservationId)
            ?? throw new ReservationNotFoundException("Parent reservation not found.");

        if (reservation.CustomerId != userId)
        {
            throw new ActionForbiddenException("Only the reservation owner can perform this action.");
        }
    }

    public async Task<GuestSession> CreateGroupOwnerSessionAsync(string userId, string groupReservationId)
    {
        var session = new GuestSession()
        {
            Id = Guid.NewGuid().ToString(),
            GroupReservationId = groupReservationId,
            GuestName = "Owner",
            Dishes = [],
            IsOwnerSession = true,
            OwnerUserId = userId,
            BillType = BillType.Shared,
        };

        await guestSessionRepository.SaveAsync(session);

        return session;
    }

    public async Task<GetGuestSessionResponse> GetGuestSessionInfoAsync(string guestSessionId)
    {
        var session = await guestSessionRepository.GetByGuestSessionId(guestSessionId)
            ?? throw new GuestSessionNotFoundException("Guest session was not found.");

        var dishIds = session.Dishes
            .Select(s => s.DishId)
            .Distinct()
            .ToArray();

        var allDishes = await dishRepository.GetDishesByIdsAsync(dishIds);
        var dishesById = allDishes.ToDictionary(d => d.Id);

        var items = session.Dishes.Select(d => new OrderItemResponse
        {
            Dish = DishMapper.ToDishResponse(dishesById[d.DishId]),
            Quantity = d.Quantity
        }).ToArray();

        decimal total = items.Sum(i => i.Quantity * i.Dish.Price);

        string name = session.GuestName;
        if (session.IsOwnerSession)
        {
            var groupReservation = await groupReservationRepository.GetGroupReservationByIdAsync(session.GroupReservationId);
            var reservation = await reservationRepository.GetReservationByIdAsync(groupReservation!.ReservationId);
            var user = await userRepository.GetUserByIdAsync(reservation!.CustomerId!);
            name = user!.Attributes.First(a => a.Name == "given_name").Value;
        }

        return new GetGuestSessionResponse(name, items, total, session.IsSubmitted, GetBillType(session));
    }

}





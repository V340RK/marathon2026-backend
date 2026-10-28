using APIHandler.Constants;
using APIHandler.Constants.Dishes;
using APIHandler.Constants.Orders;
using APIHandler.Entities;
using APIHandler.Exceptions;
using APIHandler.Models.GuestSessions;
using APIHandler.Repositories.Auth;
using APIHandler.Repositories.Dishes;
using APIHandler.Repositories.GroupReservations;
using APIHandler.Repositories.GuestSessions;
using APIHandler.Repositories.Orders;
using APIHandler.Repositories.Reservations;
using APIHandler.Repositories.Tables;
using APIHandler.Services.GuestSessions;
using Moq;

namespace APIHandler.Tests.Services;

// Homework: owner-side group pre-order (sessions snapshot, owner session id, submit, delete)
public class GuestSessionServiceOwnerTests
{
    private const string OwnerId = "owner-user";
    private const string GroupId = "group-1";
    private const string ReservationId = "reservation-1";

    private readonly Mock<IGuestSessionRepository> _sessions = new();
    private readonly Mock<IGroupReservationRepository> _groups = new();
    private readonly Mock<IDishRepository> _dishes = new();
    private readonly Mock<IReservationRepository> _reservations = new();
    private readonly Mock<IOrderRepository> _orders = new();
    private readonly GuestSessionService _service;

    private readonly GroupReservation _group = new()
    {
        GroupReservationId = GroupId,
        ReservationId = ReservationId,
        ReservationToken = "token",
        Status = GroupReservationStatus.Open,
    };

    public GuestSessionServiceOwnerTests()
    {
        _service = new GuestSessionService(
            _groups.Object,
            _sessions.Object,
            _dishes.Object,
            _reservations.Object,
            new Mock<IUserRepository>().Object,
            new Mock<ITableRepository>().Object,
            _orders.Object);

        _groups.Setup(r => r.GetGroupReservationByIdAsync(GroupId)).ReturnsAsync(_group);
        _groups.Setup(r => r.GetByReservationIdAsync(ReservationId)).ReturnsAsync(_group);
        _reservations.Setup(r => r.GetReservationByIdAsync(ReservationId)).ReturnsAsync(Reservation(OwnerId));
        _dishes.Setup(r => r.GetDishesByIdsAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync([Dish("dish1", 180), Dish("dish2", 220)]);
    }

    // ---------- GET /guest/groups/{id}/sessions ----------

    [Fact]
    public async Task GetAllSessionsAsync_Owner_ReturnsParticipantsWithTotals()
    {
        SetupSessions(
            Session("s1", "Andrii", submitted: true, ("dish1", 2)),
            Session("s2", "Olena", submitted: false, ("dish2", 1)));

        var result = await _service.GetAllSessionsAsync(OwnerId, GroupId);

        var participants = result.Participants.ToList();
        Assert.Equal(2, participants.Count);
        Assert.Equal(360, participants[0].Total);
        Assert.True(participants[0].IsSubmitted);
        Assert.Equal(220, participants[1].Total);
        Assert.Equal(580, result.Total);
    }

    [Fact]
    public async Task GetAllSessionsAsync_OwnerSession_IsMarkedAsOwner()
    {
        var owner = Session("o1", "Owner", submitted: false);
        owner.IsOwnerSession = true;
        SetupSessions(owner);

        var result = await _service.GetAllSessionsAsync(OwnerId, GroupId);

        Assert.Equal(ParticipantType.Owner, result.Participants.Single().Type);
        Assert.Equal(0, result.Total);
    }

    [Fact]
    public async Task GetAllSessionsAsync_NotOwner_ThrowsForbidden()
    {
        await Assert.ThrowsAsync<ActionForbiddenException>(() => _service.GetAllSessionsAsync("someone-else", GroupId));
    }

    [Fact]
    public async Task GetAllSessionsAsync_GroupNotFound_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<GroupReservationNotFoundException>(() => _service.GetAllSessionsAsync(OwnerId, "missing"));
    }

    // ---------- GET /reservations/{id}/guest-session-id ----------

    [Fact]
    public async Task GetOwnerGuestSessionIdAsync_ExistingOwnerSession_ReturnsItsId()
    {
        var owner = Session("owner-session-123", "Owner", submitted: false);
        owner.IsOwnerSession = true;
        owner.OwnerUserId = OwnerId;
        SetupSessions(Session("s1", "Guest", submitted: false), owner);

        var result = await _service.GetOwnerGuestSessionIdAsync(OwnerId, ReservationId);

        Assert.Equal("owner-session-123", result.GuestSessionId);
        _sessions.Verify(r => r.SaveAsync(It.IsAny<GuestSession>()), Times.Never);
    }

    [Fact]
    public async Task GetOwnerGuestSessionIdAsync_NoOwnerSession_CreatesOne()
    {
        SetupSessions(Session("s1", "Guest", submitted: false));

        var result = await _service.GetOwnerGuestSessionIdAsync(OwnerId, ReservationId);

        _sessions.Verify(r => r.SaveAsync(It.Is<GuestSession>(s =>
            s.Id == result.GuestSessionId && s.IsOwnerSession && s.OwnerUserId == OwnerId && s.GroupReservationId == GroupId)), Times.Once);
    }

    [Fact]
    public async Task GetOwnerGuestSessionIdAsync_NotOwner_ThrowsForbidden()
    {
        await Assert.ThrowsAsync<ActionForbiddenException>(() => _service.GetOwnerGuestSessionIdAsync("someone-else", ReservationId));
    }

    [Fact]
    public async Task GetOwnerGuestSessionIdAsync_ReservationNotFound_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<ReservationNotFoundException>(() => _service.GetOwnerGuestSessionIdAsync(OwnerId, "missing"));
    }

    [Fact]
    public async Task GetOwnerGuestSessionIdAsync_GroupNotFound_ThrowsNotFound()
    {
        _groups.Setup(r => r.GetByReservationIdAsync(ReservationId)).ReturnsAsync((GroupReservation?)null);

        await Assert.ThrowsAsync<GroupReservationNotFoundException>(() => _service.GetOwnerGuestSessionIdAsync(OwnerId, ReservationId));
    }

    // ---------- POST /group-reservations/{id}/submit ----------

    [Fact]
    public async Task SubmitGroupPreOrderAsync_MergesSubmittedDishesAndUpdatesStatuses()
    {
        SetupSessions(
            Session("s1", "Andrii", submitted: true, ("dish1", 2), ("dish2", 1)),
            Session("s2", "Olena", submitted: true, ("dish1", 1), ("dish2", 1)),
            Session("s3", "Max", submitted: false, ("dish1", 5)));
        var order = Order(("dish2", 1));
        _orders.Setup(r => r.GetOrderByReservationIdAsync(ReservationId)).ReturnsAsync(order);

        var result = await _service.SubmitGroupPreOrderAsync(OwnerId, GroupId, ReservationId);

        Assert.Equal(3, result.Single(d => d.DishId == "dish1").Quantity); // not-submitted session ignored
        Assert.Equal(2, result.Single(d => d.DishId == "dish2").Quantity);
        Assert.Equal(3, order.Dishes.Single(d => d.DishId == "dish1").Quantity);
        Assert.Equal(3, order.Dishes.Single(d => d.DishId == "dish2").Quantity); // added to the existing item
        Assert.Equal(OrderStatus.SubmittedByCustomer, order.Status);
        Assert.Equal(GroupReservationStatus.Submitted, _group.Status);
        _orders.Verify(r => r.SaveOrderAsync(order), Times.Once);
        _groups.Verify(r => r.SaveGroupReservationAsync(_group), Times.Once);
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_NoSubmittedSessions_ThrowsValidation()
    {
        SetupSessions(Session("s1", "Max", submitted: false, ("dish1", 1)));

        await Assert.ThrowsAsync<ValidationException>(() => _service.SubmitGroupPreOrderAsync(OwnerId, GroupId, ReservationId));
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_SubmittedSessionsWithoutDishes_ThrowsValidation()
    {
        SetupSessions(Session("s1", "Andrii", submitted: true));

        await Assert.ThrowsAsync<ValidationException>(() => _service.SubmitGroupPreOrderAsync(OwnerId, GroupId, ReservationId));
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_OrderNotFound_ThrowsValidation()
    {
        SetupSessions(Session("s1", "Andrii", submitted: true, ("dish1", 1)));
        _orders.Setup(r => r.GetOrderByReservationIdAsync(ReservationId)).ReturnsAsync((Order?)null);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.SubmitGroupPreOrderAsync(OwnerId, GroupId, ReservationId));
        Assert.Contains("Order", ex.Message);
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_NotOwner_ThrowsForbidden()
    {
        await Assert.ThrowsAsync<ActionForbiddenException>(() => _service.SubmitGroupPreOrderAsync("someone-else", GroupId, ReservationId));
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_GroupNotFound_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<GroupReservationNotFoundException>(() => _service.SubmitGroupPreOrderAsync(OwnerId, "missing", ReservationId));
    }

    [Fact]
    public async Task SubmitGroupPreOrderAsync_AlreadySubmitted_Throws()
    {
        _group.Status = GroupReservationStatus.Submitted;

        await Assert.ThrowsAsync<GroupReservationSubmittedException>(() => _service.SubmitGroupPreOrderAsync(OwnerId, GroupId, ReservationId));
    }

    // ---------- DELETE /guest/groups/{id}/sessions/{sessionId} ----------

    [Fact]
    public async Task DeleteGuestSessionAsync_DeletesAndReturnsUpdatedSnapshot()
    {
        var removed = Session("s1", "Andrii", submitted: true, ("dish1", 2));
        _sessions.Setup(r => r.GetByGuestSessionId("s1")).ReturnsAsync(removed);
        SetupSessions(Session("s2", "Olena", submitted: false, ("dish2", 1)));

        var result = await _service.DeleteGuestSessionAsync(OwnerId, GroupId, "s1");

        _sessions.Verify(r => r.DeleteAsync(removed), Times.Once);
        Assert.Equal("s2", result.Participants.Single().Id);
        Assert.Equal(220, result.Total);
    }

    [Fact]
    public async Task DeleteGuestSessionAsync_SessionFromAnotherGroup_ThrowsValidation()
    {
        var foreign = Session("s9", "Stranger", submitted: false);
        foreign.GroupReservationId = "other-group";
        _sessions.Setup(r => r.GetByGuestSessionId("s9")).ReturnsAsync(foreign);

        await Assert.ThrowsAsync<ValidationException>(() => _service.DeleteGuestSessionAsync(OwnerId, GroupId, "s9"));
        _sessions.Verify(r => r.DeleteAsync(It.IsAny<GuestSession>()), Times.Never);
    }

    [Fact]
    public async Task DeleteGuestSessionAsync_SessionNotFound_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<GuestSessionNotFoundException>(() => _service.DeleteGuestSessionAsync(OwnerId, GroupId, "missing"));
    }

    // ---------- Bill type: "I pay / Guest pays" (own extension of the feature) ----------

    [Fact]
    public async Task GetAllSessionsAsync_SplitsTotalsBySharedAndIndividualBills()
    {
        var owner = Session("o1", "Owner", submitted: false, ("dish1", 1));
        owner.IsOwnerSession = true;
        var treated = Session("s1", "Olha", submitted: true, ("dish2", 1));
        treated.BillType = BillType.Shared;
        var paysSelf = Session("s2", "Dmytro", submitted: true, ("dish1", 2));
        SetupSessions(owner, treated, paysSelf);

        var result = await _service.GetAllSessionsAsync(OwnerId, GroupId);

        Assert.Equal(BillType.Shared, result.Participants.Single(p => p.Id == "o1").BillType); // owner is always shared
        Assert.Equal(BillType.Individual, result.Participants.Single(p => p.Id == "s2").BillType);
        Assert.Equal(400, result.SharedTotal);     // 180 + 220
        Assert.Equal(360, result.IndividualTotal); // 2 x 180
        Assert.Equal(760, result.Total);
    }

    [Fact]
    public async Task SetGuestBillTypeAsync_Owner_ChangesGuestBillAndReturnsSnapshot()
    {
        var guest = Session("s1", "Olha", submitted: false, ("dish2", 1));
        _sessions.Setup(r => r.GetByGuestSessionId("s1")).ReturnsAsync(guest);
        SetupSessions(guest);

        var result = await _service.SetGuestBillTypeAsync(OwnerId, GroupId, "s1", BillType.Shared);

        Assert.Equal(BillType.Shared, guest.BillType);
        _sessions.Verify(r => r.SaveAsync(guest), Times.Once);
        Assert.Equal(220, result.SharedTotal);
    }

    [Fact]
    public async Task SetGuestBillTypeAsync_SameValue_DoesNotSave()
    {
        var guest = Session("s1", "Olha", submitted: false);
        _sessions.Setup(r => r.GetByGuestSessionId("s1")).ReturnsAsync(guest);
        SetupSessions(guest);

        await _service.SetGuestBillTypeAsync(OwnerId, GroupId, "s1", BillType.Individual);

        _sessions.Verify(r => r.SaveAsync(It.IsAny<GuestSession>()), Times.Never);
    }

    [Fact]
    public async Task SetGuestBillTypeAsync_NotOwner_ThrowsForbidden()
    {
        await Assert.ThrowsAsync<ActionForbiddenException>(() => _service.SetGuestBillTypeAsync("someone-else", GroupId, "s1", BillType.Shared));
    }

    [Fact]
    public async Task SetGuestBillTypeAsync_OwnerSession_ThrowsValidation()
    {
        var owner = Session("o1", "Owner", submitted: false);
        owner.IsOwnerSession = true;
        _sessions.Setup(r => r.GetByGuestSessionId("o1")).ReturnsAsync(owner);

        await Assert.ThrowsAsync<ValidationException>(() => _service.SetGuestBillTypeAsync(OwnerId, GroupId, "o1", BillType.Individual));
    }

    [Fact]
    public async Task SetGuestBillTypeAsync_GroupAlreadySubmitted_Throws()
    {
        _group.Status = GroupReservationStatus.Submitted;

        await Assert.ThrowsAsync<GroupReservationSubmittedException>(() => _service.SetGuestBillTypeAsync(OwnerId, GroupId, "s1", BillType.Shared));
    }

    [Fact]
    public async Task SetGuestBillTypeAsync_SessionFromAnotherGroup_ThrowsValidation()
    {
        var foreign = Session("s9", "Stranger", submitted: false);
        foreign.GroupReservationId = "other-group";
        _sessions.Setup(r => r.GetByGuestSessionId("s9")).ReturnsAsync(foreign);

        await Assert.ThrowsAsync<ValidationException>(() => _service.SetGuestBillTypeAsync(OwnerId, GroupId, "s9", BillType.Shared));
    }

    [Fact]
    public async Task PayOwnOrderAsync_SharedGuest_BecomesIndividual()
    {
        var guest = Session("s1", "Olha", submitted: true);
        guest.BillType = BillType.Shared;
        _sessions.Setup(r => r.GetByGuestSessionId("s1")).ReturnsAsync(guest);

        await _service.PayOwnOrderAsync("s1");

        Assert.Equal(BillType.Individual, guest.BillType);
        _sessions.Verify(r => r.SaveAsync(guest), Times.Once);
    }

    [Fact]
    public async Task PayOwnOrderAsync_AlreadyIndividual_DoesNothing()
    {
        var guest = Session("s1", "Olha", submitted: false);
        _sessions.Setup(r => r.GetByGuestSessionId("s1")).ReturnsAsync(guest);

        await _service.PayOwnOrderAsync("s1");

        _sessions.Verify(r => r.SaveAsync(It.IsAny<GuestSession>()), Times.Never);
    }

    [Fact]
    public async Task PayOwnOrderAsync_GroupAlreadySubmitted_Throws()
    {
        var guest = Session("s1", "Olha", submitted: true);
        guest.BillType = BillType.Shared;
        _sessions.Setup(r => r.GetByGuestSessionId("s1")).ReturnsAsync(guest);
        _group.Status = GroupReservationStatus.Submitted;

        await Assert.ThrowsAsync<GroupReservationSubmittedException>(() => _service.PayOwnOrderAsync("s1"));
        Assert.Equal(BillType.Shared, guest.BillType);
    }

    [Fact]
    public async Task GetOwnerGuestSessionIdAsync_CreatedOwnerSession_IsOnSharedBill()
    {
        SetupSessions();

        await _service.GetOwnerGuestSessionIdAsync(OwnerId, ReservationId);

        _sessions.Verify(r => r.SaveAsync(It.Is<GuestSession>(s => s.IsOwnerSession && s.BillType == BillType.Shared)), Times.Once);
    }

    // ---------- helpers ----------

    private void SetupSessions(params GuestSession[] sessions) =>
        _sessions.Setup(r => r.GetAllByGroupReservationIdAsync(GroupId)).ReturnsAsync(sessions.ToList());

    private static GuestSession Session(string id, string name, bool submitted, params (string DishId, int Quantity)[] dishes) => new()
    {
        Id = id,
        GroupReservationId = GroupId,
        GuestName = name,
        IsSubmitted = submitted,
        Dishes = dishes.Select(d => new OrderItem { DishId = d.DishId, Quantity = d.Quantity, State = OrderItemState.Pending }).ToList(),
    };

    private static Dish Dish(string id, decimal price) => new()
    {
        Id = id,
        Name = id,
        Price = price,
        State = DishState.Available,
        LocationId = "loc-1",
    };

    private static Reservation Reservation(string customerId) => new()
    {
        Id = ReservationId,
        TableId = "table-1",
        WaiterId = "waiter-1",
        CustomerId = customerId,
        GuestCount = 4,
        Status = ReservationStatus.Reserved,
    };

    private static Order Order(params (string DishId, int Quantity)[] dishes) => new()
    {
        OrderId = "order-1",
        CustomerId = OwnerId,
        ReservationId = ReservationId,
        ReservationAddress = "Khreshchatyk St, 22, Table 2",
        ReservationTime = "Oct 30, 2026, 7:00 p.m.",
        Dishes = dishes.Select(d => new OrderItem { DishId = d.DishId, Quantity = d.Quantity, State = OrderItemState.Pending }).ToList(),
        Status = OrderStatus.Created,
    };
}

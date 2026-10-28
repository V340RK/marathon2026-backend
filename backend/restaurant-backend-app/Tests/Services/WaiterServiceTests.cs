using Amazon.CognitoIdentityProvider.Model;
using APIHandler.Entities;
using APIHandler.Exceptions;
using APIHandler.Models.Users;
using APIHandler.Models.WaiterList;
using APIHandler.Repositories.Auth;
using APIHandler.Repositories.Locations;
using APIHandler.Repositories.Waiters;
using APIHandler.Services.Auth;
using APIHandler.Services.Waiters;
using AutoFixture.Xunit2;
using Moq;

namespace APIHandler.Tests.Services;

public class WaiterServiceTests
{
    private readonly Mock<IWaiterRepository> _mockWaiterRepository;
    private readonly Mock<ILocationRepository> _mockLocationRepository;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly WaiterService _waiterService;

    public WaiterServiceTests()
    {
        _mockWaiterRepository = new Mock<IWaiterRepository>();
        _mockLocationRepository = new Mock<ILocationRepository>();
        _mockUserService = new Mock<IUserService>();
        _mockUserRepository = new Mock<IUserRepository>();

        _waiterService = new WaiterService(
            _mockWaiterRepository.Object,
            _mockLocationRepository.Object,
            _mockUserService.Object,
            _mockUserRepository.Object
        );
    }

    [Theory]
    [AutoData]
    public async Task AddWaiterToListAsync_ShouldAddWaiter_WhenValid(Location location)
    {
        location.Id = "loc-1";
        var request = new UpdateWaiterListRequest { Email = "test@test.com", LocationId = "loc-1" };
        var userResponse = new AdminGetUserResponse
        {
            UserAttributes = new List<AttributeType> { new AttributeType { Name = "sub", Value = "user-1" } }
        };

        _mockLocationRepository.Setup(r => r.GetLocationByIdAsync(request.LocationId)).ReturnsAsync(location);
        _mockUserRepository.Setup(r => r.GetUserByEmailAsync(request.Email)).ReturnsAsync(userResponse);

        await _waiterService.AddWaiterToListAsync(request);

        _mockWaiterRepository.Verify(r => r.SaveWaiterAsync(It.Is<WaiterListItem>(w => w.UserId == "user-1" && w.LocationId == "loc-1")), Times.Once);
        _mockUserRepository.Verify(r => r.EnsureGroupExistsAsync(Roles.Waiter.ToString()), Times.Once);
        _mockUserRepository.Verify(r => r.AddUserToGroupAsync(request.Email, Roles.Waiter.ToString()), Times.Once);
    }

    [Fact]
    public async Task AddWaiterToListAsync_ShouldThrowArgumentException_WhenLocationNotFound()
    {
        var request = new UpdateWaiterListRequest { Email = "test@test.com", LocationId = "loc-1" };

        _mockLocationRepository.Setup(r => r.GetLocationByIdAsync(request.LocationId)).ReturnsAsync((Location?)null);

        await Assert.ThrowsAsync<ArgumentException>(() => _waiterService.AddWaiterToListAsync(request));
    }

    [Theory]
    [AutoData]
    public async Task AddWaiterToListAsync_ShouldThrowInvalidOperation_WhenUserLacksSubAttribute(Location location)
    {
        location.Id = "loc-1";
        var request = new UpdateWaiterListRequest { Email = "test@test.com", LocationId = "loc-1" };
        var userResponse = new AdminGetUserResponse { UserAttributes = new List<AttributeType>() };

        _mockLocationRepository.Setup(r => r.GetLocationByIdAsync(request.LocationId)).ReturnsAsync(location);
        _mockUserRepository.Setup(r => r.GetUserByEmailAsync(request.Email)).ReturnsAsync(userResponse);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _waiterService.AddWaiterToListAsync(request));
    }

    [Theory]
    [AutoData]
    public async Task RemoveWaiterFromListAsync_ShouldRemoveWaiter_WhenValid(WaiterListItem waiter)
    {
        waiter.UserId = "user-1";
        var email = "test@test.com";
        var userResponse = new AdminGetUserResponse
        {
            UserAttributes = new List<AttributeType> { new AttributeType { Name = "sub", Value = "user-1" } }
        };

        _mockUserRepository.Setup(r => r.GetUserByEmailAsync(email)).ReturnsAsync(userResponse);
        _mockWaiterRepository.Setup(r => r.GetWaiterByIdAsync("user-1")).ReturnsAsync(waiter);

        await _waiterService.RemoveWaiterFromListAsync(email);

        _mockWaiterRepository.Verify(r => r.DeleteWaiterAsync(waiter), Times.Once);
        _mockUserRepository.Verify(r => r.RemoveUserFromGroupAsync(email, Roles.Waiter.ToString()), Times.Once);
    }

    [Fact]
    public async Task RemoveWaiterFromListAsync_ShouldDoNothing_WhenEmailIsEmpty()
    {
        await _waiterService.RemoveWaiterFromListAsync("");

        _mockWaiterRepository.Verify(r => r.DeleteWaiterAsync(It.IsAny<WaiterListItem>()), Times.Never);
    }

    [Fact]
    public async Task RemoveWaiterFromListAsync_ShouldDoNothing_WhenWaiterNotFoundInDb()
    {
        var email = "test@test.com";
        var userResponse = new AdminGetUserResponse
        {
            UserAttributes = new List<AttributeType> { new AttributeType { Name = "sub", Value = "user-1" } }
        };

        _mockUserRepository.Setup(r => r.GetUserByEmailAsync(email)).ReturnsAsync(userResponse);
        _mockWaiterRepository.Setup(r => r.GetWaiterByIdAsync("user-1")).ReturnsAsync((WaiterListItem?)null);

        await _waiterService.RemoveWaiterFromListAsync(email);

        _mockWaiterRepository.Verify(r => r.DeleteWaiterAsync(It.IsAny<WaiterListItem>()), Times.Never);
    }

    [Fact]
    public async Task IsWaiterInListAsync_ShouldReturnTrue_WhenWaiterExists()
    {
        var email = "test@test.com";
        var waiter = new WaiterListItem { UserId = "user-1", LocationId = "loc-1" };

        _mockUserService.Setup(s => s.GetUserIdByEmailAsync(email)).ReturnsAsync("user-1");
        _mockWaiterRepository.Setup(r => r.GetWaiterByIdAsync("user-1")).ReturnsAsync(waiter);

        var result = await _waiterService.IsWaiterInListAsync(email);

        Assert.True(result);
    }

    [Fact]
    public async Task IsWaiterInListAsync_ShouldReturnFalse_WhenEmailIsEmpty()
    {
        var result = await _waiterService.IsWaiterInListAsync("   ");

        Assert.False(result);
    }

    [Fact]
    public async Task IsWaiterInListAsync_ShouldReturnFalse_WhenUserNotFoundInCognito()
    {
        var email = "test@test.com";
        _mockUserService.Setup(s => s.GetUserIdByEmailAsync(email)).ReturnsAsync((string?)null);

        var result = await _waiterService.IsWaiterInListAsync(email);

        Assert.False(result);
    }

    [Fact]
    public async Task IsWaiterInListAsync_ShouldReturnFalse_WhenWaiterNotInDb()
    {
        var email = "test@test.com";
        _mockUserService.Setup(s => s.GetUserIdByEmailAsync(email)).ReturnsAsync("user-1");
        _mockWaiterRepository.Setup(r => r.GetWaiterByIdAsync("user-1")).ReturnsAsync((WaiterListItem?)null);

        var result = await _waiterService.IsWaiterInListAsync(email);

        Assert.False(result);
    }

    [Fact]
    public async Task FindWaiterByReservationIdAsync_ShouldReturnWaiter_WhenFound()
    {
        var reservationId = "res-1";
        var waiter = new WaiterListItem { UserId = "user-1", Reservations = new List<string> { reservationId }, LocationId = "loc-1" };

        _mockWaiterRepository.Setup(r => r.GetAllWaitersAsync()).ReturnsAsync(new List<WaiterListItem> { waiter });

        var result = await _waiterService.FindWaiterByReservationIdAsync(reservationId);

        Assert.NotNull(result);
        Assert.Equal("user-1", result.UserId);
    }

    [Fact]
    public async Task FindWaiterByReservationIdAsync_ShouldReturnNull_WhenNotFound()
    {
        var reservationId = "res-1";
        var waiter = new WaiterListItem { UserId = "user-1", Reservations = new List<string> { "other-res" }, LocationId = "loc-1" };

        _mockWaiterRepository.Setup(r => r.GetAllWaitersAsync()).ReturnsAsync(new List<WaiterListItem> { waiter });

        var result = await _waiterService.FindWaiterByReservationIdAsync(reservationId);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindWaiterByIdAsync_ShouldReturnWaiter_WhenFound()
    {
        var userId = "user-1";
        var waiter = new WaiterListItem { UserId = userId, LocationId = "loc-1" };

        _mockWaiterRepository.Setup(r => r.GetWaiterByIdAsync(userId)).ReturnsAsync(waiter);

        var result = await _waiterService.FindWaiterByIdAsync(userId);

        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
    }

    [Fact]
    public async Task FindWaiterByIdAsync_ShouldThrowValidationException_WhenNotFound()
    {
        var userId = "user-1";
        _mockWaiterRepository.Setup(r => r.GetWaiterByIdAsync(userId)).ReturnsAsync((WaiterListItem?)null);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _waiterService.FindWaiterByIdAsync(userId));
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task GetLeastBusyWaiterAsync_ShouldReturnWaiterWithFewestReservations()
    {
        var locationId = "loc-1";
        var busyWaiter = new WaiterListItem { UserId = "busy", Reservations = new List<string> { "r1", "r2" }, LocationId = locationId };
        var freeWaiter = new WaiterListItem { UserId = "free", Reservations = new List<string> { "r3" }, LocationId = locationId };

        _mockWaiterRepository.Setup(r => r.GetWaitersByLocationIdAsync(locationId))
            .ReturnsAsync(new List<WaiterListItem> { busyWaiter, freeWaiter });

        var result = await _waiterService.GetLeastBusyWaiterAsync(locationId);

        Assert.Equal("free", result.UserId);
    }

    [Fact]
    public async Task GetLeastBusyWaiterAsync_ShouldThrowValidationException_WhenNoWaitersFound()
    {
        var locationId = "loc-1";
        _mockWaiterRepository.Setup(r => r.GetWaitersByLocationIdAsync(locationId)).ReturnsAsync(new List<WaiterListItem>());

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _waiterService.GetLeastBusyWaiterAsync(locationId));
        Assert.Contains("No waiters found", exception.Message);
    }

    [Fact]
    public async Task UpdateWaiterReservationsAsync_ShouldUpdateReservations_WhenValid()
    {
        var userId = "user-1";
        var reservationIds = new List<string> { "res-1", "res-2" };
        var waiter = new WaiterListItem { UserId = userId, Reservations = new List<string>(), LocationId = "loc-1" };

        _mockWaiterRepository.Setup(r => r.GetWaiterByIdAsync(userId)).ReturnsAsync(waiter);

        await _waiterService.UpdateWaiterReservationsAsync(userId, reservationIds);

        Assert.Equal(2, waiter.Reservations.Count);
        _mockWaiterRepository.Verify(r => r.SaveWaiterAsync(waiter), Times.Once);
    }

    [Fact]
    public async Task UpdateWaiterReservationsAsync_ShouldThrowInvalidOperation_WhenWaiterNotFound()
    {
        var userId = "user-1";
        var reservationIds = new List<string> { "res-1" };

        _mockWaiterRepository.Setup(r => r.GetWaiterByIdAsync(userId)).ReturnsAsync((WaiterListItem?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _waiterService.UpdateWaiterReservationsAsync(userId, reservationIds));
    }

    [Fact]
    public async Task GetWaiterLocationIdAsync_ShouldReturnLocationId_WhenFound()
    {
        var waiterId = "waiter-1";
        var waiter = new WaiterListItem { UserId = waiterId, LocationId = "loc-1" };

        _mockWaiterRepository.Setup(r => r.GetWaiterByIdAsync(waiterId)).ReturnsAsync(waiter);

        var result = await _waiterService.GetWaiterLocationIdAsync(waiterId);

        Assert.Equal("loc-1", result);
    }

    [Fact]
    public async Task GetWaiterLocationIdAsync_ShouldThrowArgumentException_WhenWaiterIdIsEmpty()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _waiterService.GetWaiterLocationIdAsync(""));
    }

    [Fact]
    public async Task GetWaiterLocationIdAsync_ShouldReturnNull_WhenNotFound()
    {
        var waiterId = "waiter-1";
        _mockWaiterRepository.Setup(r => r.GetWaiterByIdAsync(waiterId)).ReturnsAsync((WaiterListItem?)null);

        var result = await _waiterService.GetWaiterLocationIdAsync(waiterId);

        Assert.Null(result);
    }
}
using APIHandler.Entities;
using APIHandler.Exceptions;
using APIHandler.Models.Locations;
using APIHandler.Models.WaiterList;
using APIHandler.Repositories.Locations;
using APIHandler.Repositories.Tables;
using APIHandler.Services.Locations;
using APIHandler.Services.Waiters;
using AutoFixture.Xunit2;
using Moq;

namespace APIHandler.Tests.Services;

public class LocationServiceTests
{
    private readonly Mock<ILocationRepository> _mockLocationRepository = new();
    private readonly Mock<IWaiterService> _mockWaiterService = new();
    private readonly Mock<ITableRepository> _mockTableRepository = new();

    private readonly LocationService _locationService;

    public LocationServiceTests()
    {
        _locationService = new LocationService(
            _mockLocationRepository.Object,
            _mockWaiterService.Object,
            _mockTableRepository.Object);
    }

    [Theory]
    [AutoData]
    public async Task UpdateLocationAsync_ValidData_UpdatesAndSaves(
        string locationId,
        Location existingLocation,
        CreateLocationRequest request,
        string newImageUrl)
    {
        var originalRating = existingLocation.Rating;
        _mockLocationRepository
            .Setup(r => r.GetLocationByIdAsync(locationId))
            .ReturnsAsync(existingLocation);

        var result = await _locationService.UpdateLocationAsync(locationId, request, newImageUrl);

        Assert.Equal(request.Address, result.Address);
        Assert.Equal(request.Description, result.Description);
        Assert.Equal(request.TotalCapacity, result.TotalCapacity);
        Assert.Equal(request.AverageOccupancy, result.AverageOccupancy);
        Assert.Equal(newImageUrl, result.ImageUrl);

        // Ensure rating was NOT overwritten by the request
        Assert.Equal(originalRating, result.Rating);

        _mockLocationRepository.Verify(r => r.SaveLocationAsync(existingLocation), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task UpdateLocationAsync_LocationNotFound_ThrowsValidationException(
        string locationId,
        CreateLocationRequest request)
    {
        _mockLocationRepository
            .Setup(r => r.GetLocationByIdAsync(locationId))
            .ReturnsAsync((Location?)null);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            _locationService.UpdateLocationAsync(locationId, request, null));

        Assert.Contains("not found", exception.Message);
        _mockLocationRepository.Verify(r => r.SaveLocationAsync(It.IsAny<Location>()), Times.Never);
    }

    [Theory]
    [AutoData]
    public async Task DeleteLocationAsync_LocationNotFound_ThrowsValidationException(string locationId)
    {
        _mockLocationRepository
            .Setup(r => r.GetLocationByIdAsync(locationId))
            .ReturnsAsync((Location?)null);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            _locationService.DeleteLocationAsync(locationId, false));

        Assert.Contains("not found", exception.Message);
        _mockLocationRepository.Verify(r => r.DeleteLocationAsync(It.IsAny<Location>()), Times.Never);
    }

    [Theory]
    [AutoData]
    public async Task DeleteLocationAsync_NoDependencies_ForceFalse_DeletesSuccessfully(
        string locationId,
        Location location)
    {
        _mockLocationRepository.Setup(r => r.GetLocationByIdAsync(locationId)).ReturnsAsync(location);
        _mockLocationRepository.Setup(r => r.GetTablesByLocationIdAsync(locationId)).ReturnsAsync([]);
        _mockWaiterService.Setup(s => s.GetWaitersByLocationIdAsync(locationId)).ReturnsAsync([]);

        await _locationService.DeleteLocationAsync(locationId, force: false);

        _mockLocationRepository.Verify(r => r.DeleteLocationAsync(location), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task DeleteLocationAsync_HasTables_ForceFalse_ThrowsValidationException(
        string locationId,
        Location location,
        List<Table> tables)
    {
        _mockLocationRepository.Setup(r => r.GetLocationByIdAsync(locationId)).ReturnsAsync(location);
        _mockLocationRepository.Setup(r => r.GetTablesByLocationIdAsync(locationId)).ReturnsAsync(tables);
        _mockWaiterService.Setup(s => s.GetWaitersByLocationIdAsync(locationId)).ReturnsAsync([]);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            _locationService.DeleteLocationAsync(locationId, force: false));

        Assert.Contains("has active tables", exception.Message);
        _mockLocationRepository.Verify(r => r.DeleteLocationAsync(It.IsAny<Location>()), Times.Never);
    }

    [Theory]
    [AutoData]
    public async Task DeleteLocationAsync_HasWaiters_ForceFalse_ThrowsValidationException(
        string locationId,
        Location location,
        List<WaiterListItem> waiters)
    {
        _mockLocationRepository.Setup(r => r.GetLocationByIdAsync(locationId)).ReturnsAsync(location);
        _mockLocationRepository.Setup(r => r.GetTablesByLocationIdAsync(locationId)).ReturnsAsync([]);
        _mockWaiterService.Setup(s => s.GetWaitersByLocationIdAsync(locationId)).ReturnsAsync(waiters);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            _locationService.DeleteLocationAsync(locationId, force: false));

        Assert.Contains("assigned waiters", exception.Message);
        _mockLocationRepository.Verify(r => r.DeleteLocationAsync(It.IsAny<Location>()), Times.Never);
    }

    [Theory]
    [AutoData]
    public async Task DeleteLocationAsync_HasDependencies_ForceTrue_CascadesAndDeletes(
        string locationId,
        Location location,
        List<Table> tables,
        List<WaiterListItem> waiters)
    {
        _mockLocationRepository.Setup(r => r.GetLocationByIdAsync(locationId)).ReturnsAsync(location);
        _mockLocationRepository.Setup(r => r.GetTablesByLocationIdAsync(locationId)).ReturnsAsync(tables);
        _mockWaiterService.Setup(s => s.GetWaitersByLocationIdAsync(locationId)).ReturnsAsync(waiters);

        await _locationService.DeleteLocationAsync(locationId, force: true);

        foreach (var table in tables)
        {
            _mockTableRepository.Verify(r => r.DeleteTableAsync(table), Times.Once);
        }

        foreach (var waiter in waiters)
        {
            _mockWaiterService.Verify(s => s.RemoveWaiterByIdAsync(waiter.UserId), Times.Once);
        }

        _mockLocationRepository.Verify(r => r.DeleteLocationAsync(location), Times.Once);
    }
}
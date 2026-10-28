using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using APIHandler.Constants;
using APIHandler.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APIHandler.Repositories.Dishes;

public class DishRepository : IDishRepository
{
    private static readonly string DishTableName = ResourceNames.DishesTableName;
    private static readonly string LocationsTableName = ResourceNames.LocationsTableName;
    private readonly IDynamoDBContext _dynamoDbContext;

    public DishRepository(IDynamoDBContext dynamoDbContext)
    {
        _dynamoDbContext = dynamoDbContext ?? throw new ArgumentNullException(nameof(dynamoDbContext));
    }

    public async Task<Dish> GetDishByIdAsync(string dishId)
    {
        var dishLoadConfig = DynamoConfig.LoadConfig(DishTableName);
        return await _dynamoDbContext.LoadAsync<Dish>(dishId, dishLoadConfig);
    }

    public async Task<IEnumerable<Dish>> GetDishesByIdsAsync(IEnumerable<string> dishIds)
    {
        if (dishIds is null || !dishIds.Any())
        {
            return new List<Dish>();
        }

        var batchGet = _dynamoDbContext.CreateBatchGet<Dish>(
            new BatchGetConfig
            {
                ConsistentRead = true,
                OverrideTableName = DishTableName
            });

        foreach (var id in dishIds)
        {
            batchGet.AddKey(id);
        }

        await batchGet.ExecuteAsync();
        return batchGet.Results;
    }

    public async Task<IEnumerable<Dish>> GetDishesByLocationIdAsync(string locationId)
    {
        var resultDishes = new Dictionary<string, Dish>();

        if (!string.IsNullOrWhiteSpace(locationId))
        {
            var location = await _dynamoDbContext.LoadAsync<Location>(
                locationId,
                DynamoConfig.LoadConfig(LocationsTableName));

            if (location?.SpecialtyDishIds is { Count: > 0 })
            {
                var specialtyDishes = await GetDishesByIdsAsync(location.SpecialtyDishIds);
                foreach (var dish in specialtyDishes)
                {
                    if (dish != null && !string.IsNullOrWhiteSpace(dish.Id))
                    {
                        resultDishes[dish.Id] = dish;
                    }
                }
            }

            var scanConditions = new List<ScanCondition>
            {
                new(nameof(Dish.LocationId), ScanOperator.Equal, locationId)
            };

            var directDishes = await _dynamoDbContext
                .ScanAsync<Dish>(scanConditions, DynamoConfig.ScanConfig(DishTableName))
                .GetRemainingAsync();

            foreach (var dish in directDishes)
            {
                if (dish != null && !string.IsNullOrWhiteSpace(dish.Id))
                {
                    resultDishes[dish.Id] = dish;
                }
            }
        }

        if (resultDishes.Count == 0)
        {
            var allDishes = await _dynamoDbContext
                .ScanAsync<Dish>(new List<ScanCondition>(), DynamoConfig.ScanConfig(DishTableName))
                .GetRemainingAsync();

            return allDishes;
        }

        return resultDishes.Values.ToList();
    }

    public async Task SaveDishAsync(Dish dish)
    {
        await _dynamoDbContext.SaveAsync(dish, DynamoConfig.SaveConfig(ResourceNames.DishesTableName));
    }

    public async Task DeleteDishAsync(Dish dish)
    {
        await _dynamoDbContext.DeleteAsync(dish, DynamoConfig.DeleteConfig(ResourceNames.DishesTableName));
    }
}
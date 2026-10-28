using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Constants;
using APIHandler.Constants.Dishes;
using APIHandler.Entities;
using APIHandler.Helpers;
using APIHandler.Mappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APIHandler.Handlers.Dishes;

public class GetAllDishesHandler : IApiRequestHandler
{
    private static readonly string DishesTableName = ResourceNames.DishesTableName;

    private readonly IDynamoDBContext _dbContext;

    public GetAllDishesHandler(IDynamoDBContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public static readonly DynamoDBOperationConfig _dishesConfig = new()
    {
        OverrideTableName = DishesTableName,
    };

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        string? dishType = null;
        string? sortBy = null;
        string? name = null;

        if (request.QueryStringParameters != null)
        {
            request.QueryStringParameters.TryGetValue("dishType", out dishType);
            request.QueryStringParameters.TryGetValue("sortBy", out sortBy);
            request.QueryStringParameters.TryGetValue("name", out name);
        }

        // dishes
        var dishes = await _dbContext
            .ScanAsync<Dish>(new List<ScanCondition>(), _dishesConfig)
            .GetRemainingAsync();

        var orders = new List<global::APIHandler.Entities.Order>();
        var popularityMap = new Dictionary<string, int>();

        if (sortBy == DishSortingLabels.PopularityAscending ||
            sortBy == DishSortingLabels.PopularityDescending)
        {
            orders = await _dbContext
                .ScanAsync<global::APIHandler.Entities.Order>(new List<ScanCondition>(), new DynamoDBOperationConfig
                {
                    OverrideTableName = ResourceNames.OrdersTableName
                })
                .GetRemainingAsync();

            popularityMap = orders
                .SelectMany(o => o.Dishes)
                .GroupBy(item => item.DishId)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        if (!string.IsNullOrEmpty(dishType) &&
            Enum.TryParse<DishType>(dishType, ignoreCase: true, out var parsedType))
        {
            dishes = dishes.Where(d => d.DishType == parsedType).ToList();
        }

        if (!string.IsNullOrEmpty(name))
        {
            dishes = dishes.Where(d => d.Name.Contains(name, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        dishes = SortDishes(dishes, sortBy, popularityMap);

        var response = dishes.ConvertAll(DishMapper.ToDishListResponse);
        return ResponseBuilder.CreateSuccessResponse(response);
    }


    private static List<Dish> SortDishes(List<Dish> dishes, string? sortBy, Dictionary<string, int> popularityMap)
    {
        return sortBy switch
        {
            DishSortingLabels.PriceAscending => dishes.OrderBy(d => d.Price).ToList(),
            DishSortingLabels.PriceDescending => dishes.OrderByDescending(d => d.Price).ToList(),

            DishSortingLabels.PopularityAscending => dishes
                .OrderBy(d => popularityMap.ContainsKey(d.Id) ? popularityMap[d.Id] : 0)
                .ToList(),

            DishSortingLabels.PopularityDescending => dishes
                .OrderByDescending(d => popularityMap.ContainsKey(d.Id) ? popularityMap[d.Id] : 0)
                .ToList(),

            _ => dishes
        };
    }

}
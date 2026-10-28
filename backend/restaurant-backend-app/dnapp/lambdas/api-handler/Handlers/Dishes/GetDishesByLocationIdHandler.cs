using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Constants;
using APIHandler.Entities;
using APIHandler.Helpers;
using APIHandler.Mappers;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace APIHandler.Handlers.Dishes;

public class GetDishesByLocationIdHandler : IApiRequestHandler
{
    private static readonly string LocationsTableName = ResourceNames.LocationsTableName;
    private static readonly string DishesTableName = ResourceNames.DishesTableName;

    private readonly IDynamoDBContext _dbContext;

    public GetDishesByLocationIdHandler(IDynamoDBContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    private static readonly DynamoDBOperationConfig dishesConfig = new()
    { OverrideTableName = DishesTableName };

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        if (request.PathParameters == null ||
            !request.PathParameters.TryGetValue("id", out var locationId) ||
            string.IsNullOrWhiteSpace(locationId))
        {
            return ResponseBuilder.CreateErrorResponse(400, "No location id was found in request.", context.Logger);
        }

        // getting location
        var location = await _dbContext.LoadAsync<Location>(
            locationId,
            DynamoConfig.LoadConfig(LocationsTableName));

        if (location == null)
        {
            return ResponseBuilder.CreateErrorResponse(
                (int)HttpStatusCode.NotFound,
                $"Location with id '{locationId}' was not found.",
                context.Logger);
        }

        if (location == null || location.SpecialtyDishIds == null || location.SpecialtyDishIds.Count == 0)
        {
            var emptyDishesList = new List<Dish>();
            return ResponseBuilder.CreateSuccessResponse(emptyDishesList);
        }

        // getting dishes
        var batchGet = _dbContext.CreateBatchGet<Dish>(dishesConfig);

        foreach (var dishId in location.SpecialtyDishIds)
        {
            batchGet.AddKey(dishId);
        }

        await batchGet.ExecuteAsync();
        var results = batchGet.Results;

        var dishesListResponse = results.ConvertAll(DishMapper.ToDishListResponse);

        return ResponseBuilder.CreateSuccessResponse(dishesListResponse);
    }
}
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Constants;
using APIHandler.Entities;
using APIHandler.Helpers;
using APIHandler.Mappers;
using System;
using System.Threading.Tasks;

namespace APIHandler.Handlers.Dishes;

public class GetDishByIdHandler : IApiRequestHandler
{
    private static readonly string DishesTableName = ResourceNames.DishesTableName;

    private readonly IDynamoDBContext _dbContext;

    public GetDishByIdHandler(IDynamoDBContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        if (request.PathParameters == null ||
            !request.PathParameters.TryGetValue("id", out var dishId) ||
            string.IsNullOrWhiteSpace(dishId))
        {
            return ResponseBuilder.CreateErrorResponse(400, "No dish id was found in request.", context.Logger);
        }

        var dish = await _dbContext.LoadAsync<Dish>(dishId, DynamoConfig.LoadConfig(DishesTableName));

        if (dish == null)
        {
            return ResponseBuilder.CreateErrorResponse(404, $"Dish with id '{dishId}' was not found.", context.Logger);
        }

        var dishResponse = DishMapper.ToDishResponse(dish);
        return ResponseBuilder.CreateSuccessResponse(dishResponse);
    }
}
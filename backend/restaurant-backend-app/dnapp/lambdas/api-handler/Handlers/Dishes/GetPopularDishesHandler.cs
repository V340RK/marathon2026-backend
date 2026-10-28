using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Constants;
using APIHandler.Entities;
using APIHandler.Helpers;
using APIHandler.Mappers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APIHandler.Handlers.Dishes;

public class GetPopularDishesHandler : IApiRequestHandler
{
    private static readonly string DishesTableName = ResourceNames.DishesTableName;

    private readonly IDynamoDBContext _dbContext;

    public GetPopularDishesHandler(IDynamoDBContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public static readonly DynamoDBOperationConfig _dishesConfig = new()
    {
        OverrideTableName = DishesTableName,
    };

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var dishes = await _dbContext
            .ScanAsync<Dish>(new List<ScanCondition>(), _dishesConfig)
            .GetRemainingAsync();

        var dishesListResponse = dishes.ConvertAll(DishMapper.ToDishListResponse);

        return ResponseBuilder.CreateSuccessResponse(dishesListResponse);
    }
}
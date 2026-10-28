using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Helpers;
using System;

namespace Function.Services;

public class ExceptionHandler
{
    public static APIGatewayProxyResponse Handle(Exception exception, ILambdaContext context)
    {
        context.Logger.LogError($"An error occurred while processing the request: {exception}");
        return ResponseBuilder.CreateErrorResponse(500, "Internal Server Error", context.Logger);
    }
}

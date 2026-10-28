using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Constants;
using APIHandler.Helpers;
using APIHandler.Routing;
using Function.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace APIHandler;

public partial class Function
{
    private readonly Dictionary<string, List<RouteHandler>> _routeTable;

    public Function()
    {
        _routeTable = RouteRegistry.GetAllRoutes()
            .GroupBy(r => r.Route.Method.ToUpperInvariant())
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var method = request.HttpMethod.ToUpperInvariant();
        var path = request.Path.TrimEnd('/');

        if (method == HttpMethods.OPTIONS)
        {
            return ResponseBuilder.CreateOptionsResponse();
        }

        context.Logger.LogLine($"Received request: {method} {path}");

        // Use only for debugging purposes. May contain sensitive information.
        // var shortBody = request.Body?.Length > 500 ? request.Body.Substring(0, 500) + "..." : request.Body;
        // context.Logger.LogLine($"Request body: {shortBody}");

        if (_routeTable.TryGetValue(method, out var handlers))
        {
            foreach (var handler in handlers)
            {
                var pathParams = handler.Match(path);
                if (pathParams != null)
                {
                    request.PathParameters = pathParams;

                    try
                    {
                        return await handler.Handler.HandleRequest(request, context);
                    }
                    catch (Exception exception)
                    {
                        return ExceptionHandler.Handle(exception, context);
                    }
                }
            }
        }

        return ResponseBuilder.CreateErrorResponse(404, "Not Found", context.Logger);
    }
}
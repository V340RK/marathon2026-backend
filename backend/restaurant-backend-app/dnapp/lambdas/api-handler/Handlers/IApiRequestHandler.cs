using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using System.Threading.Tasks;

namespace APIHandler.Handlers;

/// <summary>
/// Interface for handling API requests in the Lambda function.
/// </summary>
public interface IApiRequestHandler
{
    /// <summary>
    /// Handles the incoming API Gateway request and returns a response.
    /// </summary>
    /// <param name="request">The API Gateway proxy request containing the HTTP request data.</param>
    /// <param name="context">The Lambda context object providing information about the current invocation, function, and execution environment.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an <see cref="APIGatewayProxyResponse"/> 
    /// which encapsulates the HTTP response to be returned to the API Gateway, including status code, headers, and body.</returns>
    Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context);
}
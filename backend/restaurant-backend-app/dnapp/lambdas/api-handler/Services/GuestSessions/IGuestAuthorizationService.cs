using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using System.Threading.Tasks;

namespace APIHandler.Services.GuestSessions;

public interface IGuestAuthorizationService
{
    ValueTask<APIGatewayProxyResponse?> ValidateAsync(APIGatewayProxyRequest request, ILambdaContext context, string guestSessionid);
}
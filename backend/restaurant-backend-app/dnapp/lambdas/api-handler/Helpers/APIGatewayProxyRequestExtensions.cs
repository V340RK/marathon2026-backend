using Amazon.Lambda.APIGatewayEvents;
using System.Diagnostics.CodeAnalysis;

namespace APIHandler.Helpers;

public static class APIGatewayProxyRequestExtensions
{
    public static bool TryGetPathValue(this APIGatewayProxyRequest request, string path, [NotNullWhen(true)] out string? value)
    {
        value = null;
        return request.PathParameters is not null && request.PathParameters.TryGetValue(path, out value) && !string.IsNullOrEmpty(value);
    }
}

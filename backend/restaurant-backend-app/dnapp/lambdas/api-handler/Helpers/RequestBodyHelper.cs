using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace APIHandler.Helpers;

public class RequestBodyHelper
{
    private static readonly JsonSerializerOptions? s_jsonOptions = new(JsonSerializerDefaults.Web);

    public static bool TryParseAndValidate<T>(
        APIGatewayProxyRequest request,
        ILambdaContext context,
        [NotNullWhen(true)] out T? value,
        [NotNullWhen(false)] out APIGatewayProxyResponse? errorResponse)
    {
        value = default;
        errorResponse = null;

        if (string.IsNullOrEmpty(request.Body))
        {
            errorResponse = ResponseBuilder.CreateErrorResponse(400, "Body is empty", context.Logger);
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<T>(request.Body!, s_jsonOptions);
        }
        catch (JsonException)
        {
            errorResponse = ResponseBuilder.CreateErrorResponse(400, "Invalid json body", context.Logger);
            return false;
        }

        if(value is null)
        {
            errorResponse = ResponseBuilder.CreateErrorResponse(400, "Body is empty", context.Logger);
            return false;
        }

        return DataAnnotationsValidator.TryValidate(value, out errorResponse);
    }
}

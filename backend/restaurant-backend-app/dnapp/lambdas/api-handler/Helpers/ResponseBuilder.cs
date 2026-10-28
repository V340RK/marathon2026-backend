using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Exceptions;
using APIHandler.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace APIHandler.Helpers;

public static class ResponseBuilder
{
    private static readonly Dictionary<string, string> DefaultHeaders = new()
    {
        { "Content-Type", "application/json" },
        { "Access-Control-Allow-Origin", "*" },
        { "Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS" },
        { "Access-Control-Allow-Headers", "Content-Type, Authorization" }
    };

    public static APIGatewayProxyResponse CreateErrorResponse(
        int statusCode, string errorMessage, ILambdaLogger logger, Exception? ex = null)
    {
        logger.LogLine($"[ERROR] {errorMessage}");

        if (ex != null)
        {
            logger.LogError($"[{ex.GetType().Name}] Exception: {ex}");
        }

        return new APIGatewayProxyResponse
        {
            StatusCode = statusCode,
            Body = JsonSerializer.Serialize(new { error = errorMessage }),
            Headers = DefaultHeaders,
        };
    }


    public static APIGatewayProxyResponse CreateErrorCodeResponse(ValidationException exception)
        => CreateJsonResponse(400, new ErrorCodeResponse(exception.Message, exception switch
        {
            GroupReservationExpiredException => ErrorCode.GroupReservationExpired,
            GuestLimitExceededException => ErrorCode.GuestLimitExceeded,
            GroupReservationSubmittedException => ErrorCode.GroupReservationSubmitted,
            GuestSessionExpiredException => ErrorCode.GuestSessionExpired,
            _ => ErrorCode.Unknown
        }));

    public static APIGatewayProxyResponse CreateSuccessResponse(string message)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = 200,
            Body = JsonSerializer.Serialize(new { message = message }),
            Headers = DefaultHeaders,
        };
    }

    public static APIGatewayProxyResponse CreateOptionsResponse()
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = 200,
            Headers = DefaultHeaders,
        };
    }

    public static APIGatewayProxyResponse CreateSuccessResponse(object body, int statusCode = 200)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = statusCode,
            Body = JsonSerializer.Serialize(body),
            Headers = DefaultHeaders,
        };
    }

    public static APIGatewayProxyResponse CreateJsonResponse(int statusCode, object body)
    {
        var options = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return new APIGatewayProxyResponse
        {
            StatusCode = statusCode,
            Body = JsonSerializer.Serialize(body, options),
            Headers = DefaultHeaders
        };
    }

    public static APIGatewayProxyResponse CreatePlainTextResponse(int statusCode, string message)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = statusCode,
            Body = message,
            Headers = new Dictionary<string, string>
            {
                { "Content-Type", "text/plain" },
                { "Access-Control-Allow-Origin", "*" }
            }
        };
    }

    public static APIGatewayProxyResponse CreateFileResponse(
        byte[] fileContent,
        string fileName,
        string contentType)
    {
        bool isTextFormat = contentType.StartsWith("text/") || contentType == "application/json";

        return new APIGatewayProxyResponse
        {
            StatusCode = 200,
            IsBase64Encoded = !isTextFormat, // true: pdf, excel, false: csv
            Body = isTextFormat
                ? Encoding.UTF8.GetString(fileContent)
                : Convert.ToBase64String(fileContent),
            Headers = new Dictionary<string, string>
            {
                { "Content-Type", contentType },
                { "Content-Disposition", $"attachment; filename=\"{fileName}\"" },
                { "Access-Control-Allow-Origin", "*" },
                { "Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS" },
                { "Access-Control-Allow-Headers", "Content-Type, Authorization" }
            }
        };
    }
}
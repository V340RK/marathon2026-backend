using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Exceptions;
using APIHandler.Helpers;
using APIHandler.Models.Users;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace APIHandler.Handlers.Users;

public class ChangeOwnPasswordHandler : IApiRequestHandler
{
    private readonly AmazonCognitoIdentityProviderClient _cognitoClient;

    public ChangeOwnPasswordHandler(AmazonCognitoIdentityProviderClient cognitoClient)
    {
        _cognitoClient = cognitoClient ?? throw new ArgumentNullException(nameof(cognitoClient));
    }

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var (errorResponse, passwordRequest) = ValidateRequest(request, context);
        if (passwordRequest is null)
        {
            return errorResponse!;
        }

        var response = await _cognitoClient.ChangePasswordAsync(new ChangePasswordRequest
        {
            AccessToken = passwordRequest.AccessToken,
            PreviousPassword = passwordRequest.OldPassword,
            ProposedPassword = passwordRequest.NewPassword
        });

        return response.HttpStatusCode == HttpStatusCode.OK
            ? ResponseBuilder.CreateSuccessResponse("Password changed successfully.")
            : ResponseBuilder.CreateErrorResponse((int)response.HttpStatusCode, "Failed to change password.", context.Logger);
    }

    private static (APIGatewayProxyResponse?, ChangeUserPasswordRequest?) ValidateRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        ChangeUserPasswordRequest? passwordRequest;

        try
        {
            passwordRequest = JsonSerializer.Deserialize<ChangeUserPasswordRequest>(request.Body);
            if (passwordRequest is null)
            {
                context.Logger.LogError("[ERROR] Request body is null.");
                return (ResponseBuilder.CreateErrorResponse(400, "Invalid request body.", context.Logger), null);
            }
        }
        catch (JsonException)
        {
            context.Logger.LogError("[ERROR] Request body is null or invalid.");
            return (ResponseBuilder.CreateErrorResponse(400, "Invalid request body.", context.Logger), null);
        }

        try
        {
            PersonalDataValidator.ValidatePassword(passwordRequest?.OldPassword);
        }
        catch (ValidationException ex)
        {
            context.Logger.LogError($"[ERROR] {ex.Message}");
            return (ResponseBuilder.CreateErrorResponse(400, ex.Message, context.Logger), null);
        }

        return (null, passwordRequest);
    }
}

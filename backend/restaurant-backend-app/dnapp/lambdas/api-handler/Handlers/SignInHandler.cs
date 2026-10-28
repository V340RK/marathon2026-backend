using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Constants;
using APIHandler.Exceptions;
using APIHandler.Helpers;
using APIHandler.Models;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace APIHandler.Handlers;

public class SignInHandler : IApiRequestHandler
{
    private readonly AmazonCognitoIdentityProviderClient _cognitoClient;

    public SignInHandler(AmazonCognitoIdentityProviderClient cognitoClient)
    {
        _cognitoClient = cognitoClient ?? throw new ArgumentNullException(nameof(cognitoClient));
    }

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return ResponseBuilder.CreateErrorResponse(400, "Invalid request: body is empty", context.Logger);
        }

        if (!TryDeserializeRequest(request, out var signInRequest))
        {
            return ResponseBuilder.CreateErrorResponse(400, "Invalid request body format", context.Logger);
        }

        var (isValid, errorMessage) = ValidateRequest(signInRequest, context);
        if (!isValid)
            return ResponseBuilder.CreateErrorResponse(400, errorMessage, context.Logger);

        var result = await TrySignInUserAsync(signInRequest, context);
        if (result is null)
            return ResponseBuilder.CreateErrorResponse(401, "Invalid email or password", context.Logger);

        return ResponseBuilder.CreateSuccessResponse(new SignInResponse
        {
            Message = "Sign in successful",
            IdToken = result.IdToken,
            AccessToken = result.AccessToken,
        });
    }

    private async Task<AuthenticationResultType?> TrySignInUserAsync(SignInRequest signInRequest, ILambdaContext context)
    {
        var authRequest = new InitiateAuthRequest
        {
            ClientId = ResourceNames.ClientId,
            AuthFlow = AuthFlowType.USER_PASSWORD_AUTH,
            AuthParameters = new Dictionary<string, string>
            {
                { "USERNAME", signInRequest.Email },
                { "PASSWORD", signInRequest.Password }
            }
        };

        try
        {
            var response = await _cognitoClient.InitiateAuthAsync(authRequest);
            context.Logger.LogLine("User login succeeded");
            return response.AuthenticationResult;
        }
        catch (NotAuthorizedException)
        {
            context.Logger.LogLine("Invalid email or password");
            return null;
        }
    }

    private static bool TryDeserializeRequest(APIGatewayProxyRequest request, out SignInRequest? signInRequest)
    {
        try
        {
            signInRequest = JsonSerializer.Deserialize<SignInRequest>(request.Body);
            return signInRequest != null;
        }
        catch (JsonException)
        {
            signInRequest = null;
            return false;
        }
    }

    private (bool IsValid, string? ErrorMessage) ValidateRequest(SignInRequest request, ILambdaContext context)
    {
        try
        {
            PersonalDataValidator.ValidateEmail(request.Email);
            PersonalDataValidator.ValidatePassword(request.Password);
            return (true, null);
        }
        catch (ValidationException ex)
        {
            context.Logger.LogError($"Validation error: {ex.Message}");
            return (false, ex.Message);
        }
    }
}
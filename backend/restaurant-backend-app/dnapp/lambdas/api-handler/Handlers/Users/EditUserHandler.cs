using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Constants;
using APIHandler.Exceptions;
using APIHandler.Helpers;
using APIHandler.Models.Users;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace APIHandler.Handlers.Users;

public class EditUserHandler : IApiRequestHandler
{
    private readonly string _userPoolId = ResourceNames.UserPoolId;

    private readonly AmazonCognitoIdentityProviderClient _cognitoClient;

    public EditUserHandler(AmazonCognitoIdentityProviderClient cognitoClient)
    {
        _cognitoClient = cognitoClient ?? throw new ArgumentNullException(nameof(cognitoClient));
    }

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var (errorResponse, editUserRequest) = ValidateRequest(request, context);
        if (editUserRequest is null)
        {
            return errorResponse!;
        }

        if (request.RequestContext.Authorizer?.Claims is not { } claims ||
            !claims.TryGetValue("email", out var userEmail))
        {
            return ResponseBuilder.CreateErrorResponse(401, "Email not found in token claims", context.Logger);
        }

        return TryUpdateUser(editUserRequest, context.Logger, userEmail);
    }

    private APIGatewayProxyResponse TryUpdateUser(EditUserRequest editUserRequest, ILambdaLogger logger, string userEmail)
    {
        var updateRequest = new AdminUpdateUserAttributesRequest
        {
            UserPoolId = _userPoolId,
            Username = userEmail,
            UserAttributes =
            [
                new AttributeType { Name = "given_name", Value = editUserRequest.FirstName },
                new AttributeType { Name = "family_name", Value = editUserRequest.LastName }
            ]
        };

        var result = _cognitoClient.AdminUpdateUserAttributesAsync(updateRequest).Result;

        return result.HttpStatusCode == HttpStatusCode.OK
            ? ResponseBuilder.CreateSuccessResponse("User information updated successfully.")
            : ResponseBuilder
                .CreateErrorResponse((int)result.HttpStatusCode, "Failed to update user information.", logger);
    }

    private static (APIGatewayProxyResponse?, EditUserRequest?) ValidateRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        if (request.RequestContext.Authorizer?.Claims is not { } claims ||
            !claims.TryGetValue("sub", out var userId))
        {
            context.Logger.LogError("[ERROR] User is not authenticated.");
            return (ResponseBuilder.CreateErrorResponse(401, "User is not authenticated.", context.Logger), null);
        }

        EditUserRequest? editUserRequest;
        try
        {
            editUserRequest = JsonSerializer.Deserialize<EditUserRequest>(request.Body);
        }
        catch (JsonException)
        {
            context.Logger.LogError("[ERROR] Request body is null or invalid.");
            return (ResponseBuilder.CreateErrorResponse(400, "Invalid request body.", context.Logger), null);
        }

        try
        {
            PersonalDataValidator.ValidateName(editUserRequest!.FirstName, "First name");
            PersonalDataValidator.ValidateName(editUserRequest.LastName, "Last name");
        }
        catch (ValidationException e)
        {
            context.Logger.LogError($"[ERROR] {e.Message}");
            return (ResponseBuilder.CreateErrorResponse(400, e.Message, context.Logger), null);
        }

        return (null, editUserRequest);
    }
}
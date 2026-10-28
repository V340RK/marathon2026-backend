using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Constants;
using APIHandler.Exceptions;
using APIHandler.Helpers;
using APIHandler.Models;
using APIHandler.Models.Users;
using APIHandler.Services.Waiters;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace APIHandler.Handlers;

/// <summary>
/// Handles user registration requests by validating input data, registering the user with Amazon Cognito,
/// </summary>
public class RegistrationHandler : IApiRequestHandler
{
    private readonly string _userPoolId = ResourceNames.UserPoolId;
    private readonly IWaiterService _waiterService;
    private readonly AmazonCognitoIdentityProviderClient _cognitoClient;

    public RegistrationHandler(IWaiterService waiterService, AmazonCognitoIdentityProviderClient cognitoClient)
    {
        _waiterService = waiterService ?? throw new ArgumentNullException(nameof(waiterService));
        _cognitoClient = cognitoClient ?? throw new ArgumentNullException(nameof(cognitoClient));
    }

    /// <summary>
    /// Handles the incoming API Gateway request for user registration.
    /// Validates the request body, registers the user with Amazon Cognito, and confirms the user registration.
    /// </summary>
    /// <param name="request">The API Gateway proxy request containing the HTTP request data.</param>
    /// <param name="context">The Lambda context object providing information about the current invocation, function, and execution environment.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an <see cref="APIGatewayProxyResponse"/>
    /// which encapsulates the HTTP response to be returned to the API Gateway, including status code, headers, and body.</returns>
    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        if (!TryDeserializeRequest(request, out var registrationRequest))
        {
            return ResponseBuilder.CreateErrorResponse(400, "Invalid request body", context.Logger);
        }

        try
        {
            PersonalDataValidator.ValidateName(registrationRequest.FirstName, "First name");
            PersonalDataValidator.ValidateName(registrationRequest.LastName, "Last name");
            PersonalDataValidator.ValidateEmail(registrationRequest.Email);
            PersonalDataValidator.ValidatePasswords(registrationRequest.Password, registrationRequest.ConfirmPassword);
        }
        catch (ValidationException ex)
        {
            return ResponseBuilder.CreateErrorResponse(400, ex.Message, context.Logger);
        }

        var registrationError = await TryRegisterUserAsync(registrationRequest);
        if (registrationError is not null)
        {
            return ResponseBuilder.CreateErrorResponse(500, registrationError ?? "User registration failed", context.Logger);
        }

        var confirmationError = await TryConfirmUserAsync(registrationRequest.Email);
        if (confirmationError is not null)
        {
            return ResponseBuilder.CreateErrorResponse(500, confirmationError ?? "User confirmation failed", context.Logger);
        }

        var groupError = await AddUserToGroupAsync(registrationRequest.Email);
        if (groupError is not null)
        {
            return ResponseBuilder.CreateErrorResponse(500, groupError ?? "Failed to add user to group", context.Logger);
        }

        return ResponseBuilder.CreateSuccessResponse("User registered and confirmed successfully");
    }

    private static bool TryDeserializeRequest(APIGatewayProxyRequest request, out RegistrationRequest registrationRequest)
    {
        try
        {
            registrationRequest = JsonSerializer.Deserialize<RegistrationRequest>(request.Body);
            return registrationRequest != null;
        }
        catch (JsonException)
        {
            registrationRequest = null;
            return false;
        }
    }

    private async Task<string?> TryRegisterUserAsync(RegistrationRequest registrationRequest)
    {
        var signUpRequest = new SignUpRequest
        {
            ClientId = ResourceNames.ClientId,
            Username = registrationRequest.Email,
            Password = registrationRequest.Password,
            UserAttributes =
            [
                new AttributeType { Name = "email", Value = registrationRequest.Email },
                new AttributeType { Name = "given_name", Value = registrationRequest.FirstName },
                new AttributeType { Name = "family_name", Value = registrationRequest.LastName }
            ]
        };
        try
        {
            var result = await _cognitoClient.SignUpAsync(signUpRequest);
            var success = result.HttpStatusCode == HttpStatusCode.OK;
            return success ? null : "User registration failed";
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    private async Task<string?> TryConfirmUserAsync(string email)
    {
        var confirmSignUpRequest = new AdminConfirmSignUpRequest
        {
            UserPoolId = _userPoolId,
            Username = email
        };
        try
        {
            await _cognitoClient.AdminConfirmSignUpAsync(confirmSignUpRequest);
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    private async Task<string?> AddUserToGroupAsync(string email)
    {
        var groupNameStr = Roles.Customer.ToString(); // On registration, users are alwaus added to the Customer group since adding to a waiters groups requires user to be registered first.
        var groups = await _cognitoClient.ListGroupsAsync(new ListGroupsRequest { UserPoolId = _userPoolId });

        try
        {
            if (!groups.Groups.Exists(g => g.GroupName.Equals(groupNameStr, StringComparison.OrdinalIgnoreCase)))
            {
                await _cognitoClient.CreateGroupAsync(new CreateGroupRequest
                {
                    GroupName = groupNameStr,
                    UserPoolId = _userPoolId,
                    Description = $"Group for role {groupNameStr}"
                });
            }

            await _cognitoClient.AdminAddUserToGroupAsync(new AdminAddUserToGroupRequest
            {
                UserPoolId = _userPoolId,
                Username = email,
                GroupName = groupNameStr
            });
            return null;
        }
        catch (Exception ex)
        {
            return $"Failed to add user {email} to group {groupNameStr}: {ex.Message}";
        }
    }
}
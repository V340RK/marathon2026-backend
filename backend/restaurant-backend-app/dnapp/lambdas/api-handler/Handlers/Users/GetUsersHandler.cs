using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Constants;
using APIHandler.Exceptions;
using APIHandler.Helpers;
using APIHandler.Mappers;
using APIHandler.Models.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APIHandler.Handlers.Users;

public class GetUsersHandler : IApiRequestHandler
{
    private readonly string _userPoolId = ResourceNames.UserPoolId;

    private readonly AmazonCognitoIdentityProviderClient _cognitoClient;

    public GetUsersHandler(AmazonCognitoIdentityProviderClient cognitoClient)
    {
        _cognitoClient = cognitoClient ?? throw new ArgumentNullException(nameof(cognitoClient));
    }

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var (errorResponse, firstName, lastName) = ValidateRequest(request, context);
        if (errorResponse is not null)
        {
            return errorResponse;
        }

        var matchedUsers = new List<UserType>();
        string? paginationToken = null;

        do
        {
            var userRequest = new ListUsersRequest
            {
                UserPoolId = _userPoolId,
                Filter = $"given_name ^= \"{firstName}\"",
                Limit = 60,
                PaginationToken = paginationToken
            };

            var response = await _cognitoClient.ListUsersAsync(userRequest);

            if (!string.IsNullOrWhiteSpace(lastName))
            {
                response.Users = response.Users.Where(u => u.Attributes.Any(a => a.Name == "family_name"
                        && a.Value.Equals(lastName, StringComparison.OrdinalIgnoreCase))).ToList();
            }

            matchedUsers.AddRange(response.Users);
            paginationToken = response.PaginationToken;
        }
        while (!string.IsNullOrEmpty(paginationToken));

        var userDtos = matchedUsers.Select(u => u.ToUserDto());
        return ResponseBuilder.CreateSuccessResponse(userDtos);
    }

    private static (APIGatewayProxyResponse?, string, string) ValidateRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var isWaiter = false;
        if (request.RequestContext.Authorizer.Claims.TryGetValue("cognito:groups", out var groups))
        {
            var groupList = groups.Split(',').OrderBy(g => (int)Enum.Parse<Roles>(g)).ToList();
            isWaiter = groupList.Contains(Roles.Waiter.ToString());
        }

        if (!isWaiter)
        {
            return (ResponseBuilder.CreateErrorResponse(
                403, "User is not authorized to access this resource.", context.Logger), string.Empty, string.Empty);
        }

        var queryParams = request.QueryStringParameters ?? new Dictionary<string, string>();
        if (!queryParams.TryGetValue("fullName", out var fullNameStr) || string.IsNullOrWhiteSpace(fullNameStr))
        {
            return (ResponseBuilder.CreateErrorResponse(400, "Missing or empty 'fullName' query parameter.", context.Logger), string.Empty, string.Empty);
        }

        string[]? names = fullNameStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        try
        {
            if (names is null || names.Length == 0)
            {
                throw new ValidationException("Full name must be provided.");
            }

            PersonalDataValidator.ValidateName(names[0], "First name");
            if (names is { Length: > 1 })
            {
                PersonalDataValidator.ValidateName(names[1], "Last name");
            }
        }
        catch (ValidationException e)
        {
            context.Logger.LogError($"[ERROR] {e.Message}");
            return (ResponseBuilder.CreateErrorResponse(400, e.Message, context.Logger), string.Empty, string.Empty);
        }

        return (null, names[0], names.Length > 1 ? names[1] : string.Empty);
    }
}
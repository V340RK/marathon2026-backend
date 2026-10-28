using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Constants;
using APIHandler.Helpers;
using APIHandler.Models.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APIHandler.Handlers.Users;

public class GetAuthorizedUserHandler : IApiRequestHandler
{
    private readonly string UserPoolId = ResourceNames.UserPoolId;

    private readonly AmazonCognitoIdentityProviderClient _cognitoClient;

    public GetAuthorizedUserHandler(AmazonCognitoIdentityProviderClient cognitoClient)
    {
        _cognitoClient = cognitoClient ?? throw new ArgumentNullException(nameof(cognitoClient));
    }

    public GetAuthorizedUserHandler()
    {
        _cognitoClient = new AmazonCognitoIdentityProviderClient();
    }

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        if (request.RequestContext.Authorizer?.Claims == null ||
            !request.RequestContext.Authorizer.Claims.TryGetValue("sub", out var userId)
            || !request.RequestContext.Authorizer.Claims.TryGetValue("cognito:username", out var username))
        {
            return ResponseBuilder.CreateErrorResponse(401, "User is not authenticated.", context.Logger);
        }

        var response = await _cognitoClient.AdminGetUserAsync(new AdminGetUserRequest
        {
            UserPoolId = UserPoolId,
            Username = username
        });

        var attributes = response.UserAttributes.ToDictionary(attr => attr.Name, attr => attr.Value);

        var userDto = new UserDto
        {
            Id = userId,
            FirstName = attributes.GetValueOrDefault("given_name", "Unknown"),
            LastName = attributes.GetValueOrDefault("family_name", "User"),
            Email = attributes.GetValueOrDefault("email", "no_email"),
            Role = await GetUserRoleAsync(username)
        };

        return ResponseBuilder.CreateSuccessResponse(userDto);
    }

    private async Task<string> GetUserRoleAsync(string username)
    {
        var groupsResponse = await _cognitoClient.AdminListGroupsForUserAsync(new AdminListGroupsForUserRequest
        {
            Username = username,
            UserPoolId = UserPoolId
        });

        var groupNames = groupsResponse.Groups
            .Select(g => g.GroupName)
            .Where(name => Enum.TryParse<Roles>(name, out _))
            .OrderBy(name => (int)Enum.Parse<Roles>(name))
            .ToList();

        return groupNames.FirstOrDefault() ?? Roles.Customer.ToString();
    }
}
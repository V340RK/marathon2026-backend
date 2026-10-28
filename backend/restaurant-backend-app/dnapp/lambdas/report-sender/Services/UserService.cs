using Amazon.CognitoIdentityProvider;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReportSender.Services;

using Amazon.CognitoIdentityProvider.Model;

public class UserService : IUserService
{
    private static readonly string Region = Environment.GetEnvironmentVariable("Region")
        ?? throw new InvalidOperationException("Region environment variable is not set.");

    private static readonly string UserPoolId = Environment.GetEnvironmentVariable("CognitoUserPoolId")
        ?? throw new InvalidOperationException("CognitoUserPoolId environment variable is not set.");

    private readonly AmazonCognitoIdentityProviderClient _cognitoClient =
        new(Amazon.RegionEndpoint.GetBySystemName(Region));

    public async Task<List<UserType>> GetUsersByGroupAsync(string groupName)
    {
        var users = new List<UserType>();
        string? paginationToken = null;

        do
        {
            var request = new ListUsersInGroupRequest
            {
                UserPoolId = UserPoolId,
                GroupName = groupName,
                Limit = 60,
                NextToken = paginationToken
            };

            var response = await _cognitoClient.ListUsersInGroupAsync(request);

            if (response.Users != null)
                users.AddRange(response.Users);

            paginationToken = response.NextToken;

        } while (!string.IsNullOrEmpty(paginationToken));

        return users;
    }
}

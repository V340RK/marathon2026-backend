using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using APIHandler.Constants;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace APIHandler.Repositories.Auth;

public class UserRepository : IUserRepository
{
    private readonly AmazonCognitoIdentityProviderClient _cognitoClient;

    public UserRepository(AmazonCognitoIdentityProviderClient cognitoClient)
    {
        _cognitoClient = cognitoClient ?? throw new ArgumentNullException(nameof(cognitoClient));
    }

    public async Task<AdminGetUserResponse> GetUserByEmailAsync(string email)
    {
        var request = new AdminGetUserRequest
        {
            UserPoolId = ResourceNames.UserPoolId,
            Username = email.Trim().ToLower()
        };

        return await _cognitoClient.AdminGetUserAsync(request);
    }

    public async Task<UserType?> GetUserByIdAsync(string id)
    {
        var request = new ListUsersRequest()
        {
            UserPoolId = ResourceNames.UserPoolId,
            Filter = $@"sub = ""{id}"""
        };

        ListUsersResponse response = await _cognitoClient.ListUsersAsync(request);
        return response.Users.FirstOrDefault();
    }

    public async Task<ListUsersResponse> GetUsersAsync()
    {
        var request = new ListUsersRequest { UserPoolId = ResourceNames.UserPoolId };
        return await _cognitoClient.ListUsersAsync(request);
    }

    public async Task EnsureGroupExistsAsync(string groupName)
    {
        var groups = await _cognitoClient.ListGroupsAsync(new ListGroupsRequest { UserPoolId = ResourceNames.UserPoolId });
        if (!groups.Groups.Any(g => g.GroupName == groupName))
        {
            await _cognitoClient.CreateGroupAsync(new CreateGroupRequest
            {
                UserPoolId = ResourceNames.UserPoolId,
                GroupName = groupName,
                Description = $"{groupName} group"
            });
        }
    }

    public async Task AddUserToGroupAsync(string email, string groupName)
    {
        var addUserToGroupRequest = new AdminAddUserToGroupRequest
        {
            UserPoolId = ResourceNames.UserPoolId,
            Username = email.Trim().ToLower(),
            GroupName = groupName
        };

        try
        {
            await _cognitoClient.AdminAddUserToGroupAsync(addUserToGroupRequest);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to add user {email} to group {groupName}: {ex.Message}", ex);
        }
    }

    public async Task RemoveUserFromGroupAsync(string email, string groupName)
    {
        var removeUserFromGroupRequest = new AdminRemoveUserFromGroupRequest
        {
            UserPoolId = ResourceNames.UserPoolId,
            Username = email.Trim().ToLower(),
            GroupName = groupName
        };

        try
        {
            await _cognitoClient.AdminRemoveUserFromGroupAsync(removeUserFromGroupRequest);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to remove user {email} from group {groupName}: {ex.Message}", ex);
        }
    }
}
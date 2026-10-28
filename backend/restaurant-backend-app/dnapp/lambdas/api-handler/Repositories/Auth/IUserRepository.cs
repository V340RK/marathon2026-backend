using Amazon.CognitoIdentityProvider.Model;
using System.Threading.Tasks;

namespace APIHandler.Repositories.Auth;

public interface IUserRepository
{
    Task<AdminGetUserResponse> GetUserByEmailAsync(string email);
    Task<UserType?> GetUserByIdAsync(string id);
    Task<ListUsersResponse> GetUsersAsync();
    Task EnsureGroupExistsAsync(string groupName);
    Task AddUserToGroupAsync(string email, string groupName);
    Task RemoveUserFromGroupAsync(string email, string groupName);
}

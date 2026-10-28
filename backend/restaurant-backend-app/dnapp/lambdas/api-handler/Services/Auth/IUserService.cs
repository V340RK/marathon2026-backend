using APIHandler.Models.Users;
using System.Threading.Tasks;

namespace APIHandler.Services.Auth;

public interface IUserService
{
    /// <summary>
    /// Retrieves the username for a given user ID.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>The username associated with the user ID.</returns>
    Task<string?> GetUserName(string userId);

    /// <summary>
    /// Retrieves the user ID associated with a given email address.
    /// </summary>
    /// <param name="email">The email address of the user.</param>
    /// <returns>The user ID associated with the email address.</returns>
    Task<string?> GetUserIdByEmailAsync(string email);

    Task<Roles?> GetUserRoleAsync(string userId);

    Task<string?> GetUserEmail(string userId);
}
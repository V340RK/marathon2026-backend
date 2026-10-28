using Amazon.CognitoIdentityProvider.Model;
using APIHandler.Models.Users;
using APIHandler.Repositories.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APIHandler.Services.Auth;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public async Task<string?> GetUserIdByEmailAsync(string email)
    {
        try
        {
            var response = await _userRepository.GetUserByEmailAsync(email);
            var userId = response.UserAttributes.FirstOrDefault(a => a.Name == "sub")?.Value;
            return userId;
        }
        catch (UserNotFoundException)
        {
            return null;
        }
    }

    public async Task<string?> GetUserName(string userId)
    {
        try
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null) return null;

            var givenName = user.Attributes.FirstOrDefault(a => a.Name == "given_name")?.Value;
            var familyName = user.Attributes.FirstOrDefault(a => a.Name == "family_name")?.Value ?? string.Empty;

            if (string.IsNullOrEmpty(givenName) && string.IsNullOrEmpty(familyName))
            {
                return null;
            }

            return $"{givenName} {familyName}".Trim();
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<Roles?> GetUserRoleAsync(string userId)
    {
        try
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null) return null;

            var roleAttribute = user.Attributes.FirstOrDefault(a => a.Name == "cognito:groups")?.Value;

            if (string.IsNullOrEmpty(roleAttribute))
                return null;

            return roleAttribute.Split(',')
                .Select(Enum.Parse<Roles>)
                .OrderBy(r => r)
                .FirstOrDefault();
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<string?> GetUserEmail(string userId)
    {
        try
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null) return null;

            var email = user.Attributes.FirstOrDefault(a => a.Name == "email")?.Value;

            return string.IsNullOrEmpty(email) ? null : email;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
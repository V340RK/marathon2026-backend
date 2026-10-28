using Amazon.CognitoIdentityProvider.Model;
using APIHandler.Models.Users;
using System.Linq;

namespace APIHandler.Mappers;

public static class UserMapper
{
    public static UserDto ToUserDto(this UserType user)
    {
        var sub = user.Attributes.FirstOrDefault(a => a.Name == "sub")?.Value;
        return new UserDto
        {
            Id = sub ?? string.Empty,
            FirstName = user.Attributes.FirstOrDefault(a => a.Name == "given_name")?.Value ?? "Unknown",
            LastName = user.Attributes.FirstOrDefault(a => a.Name == "family_name")?.Value ?? "User",
            Email = user.Attributes.FirstOrDefault(a => a.Name == "email")?.Value ?? "no_email",
            Role = Roles.Customer.ToString()
        };
    }
}
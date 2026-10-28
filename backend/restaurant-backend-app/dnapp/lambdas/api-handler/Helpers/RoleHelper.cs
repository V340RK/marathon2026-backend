using Amazon.Lambda.APIGatewayEvents;
using APIHandler.Models.Users;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace APIHandler.Helpers;

public static class RoleHelper
{
    public static List<string> ParseRoles(string? groupsJson)
    {
        if (string.IsNullOrWhiteSpace(groupsJson))
            return new List<string>();

        try
        {
            return groupsJson.StartsWith("[") && groupsJson.EndsWith("]")
                ? JsonSerializer.Deserialize<List<string>>(groupsJson) ?? new List<string>()
                : groupsJson.Split(',').Select(r => r.Trim()).ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    public static string DetermineActiveRole(List<string> roles)
    {
        return roles.Contains(Roles.Waiter.ToString())
            ? Roles.Waiter.ToString()
            : Roles.Customer.ToString();
    }

    public static bool IsWaiter(APIGatewayProxyRequest request)
    {
        if (request.RequestContext?.Authorizer == null ||
            !request.RequestContext.Authorizer.Claims.TryGetValue("cognito:groups", out var groups) ||
            string.IsNullOrWhiteSpace(groups))
        {
            return false;
        }

        var roles = ParseRoles(groups);
        return roles.Contains(Roles.Waiter.ToString());
    }
}
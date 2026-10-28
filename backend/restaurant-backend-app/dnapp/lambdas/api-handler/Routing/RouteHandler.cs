using APIHandler.Handlers;
using APIHandler.Records;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace APIHandler.Routing;

public partial class RouteHandler
{
    private readonly Regex _routePattern;

    public Route Route { get; }
    public IApiRequestHandler Handler { get; }

    public RouteHandler(Route route, IApiRequestHandler handler)
    {
        Route = route;
        Handler = handler;

        var pattern = "^" + RoutePattern().Replace(route.Template, m => $"(?<{m.Groups[1].Value}>[^/]+)") + "$";
        _routePattern = new Regex(pattern, RegexOptions.Compiled);
    }

    public Dictionary<string, string>? Match(string path)
    {
        var match = _routePattern.Match(path);
        if (!match.Success) return null;

        var pathParams = new Dictionary<string, string>();
        foreach (var groupName in _routePattern.GetGroupNames())
        {
            if (int.TryParse(groupName, out _)) continue;
            pathParams[groupName] = match.Groups[groupName].Value;
        }

        return pathParams;
    }

    [GeneratedRegex(@"\{(\w+)\}")]
    private static partial Regex RoutePattern();
}

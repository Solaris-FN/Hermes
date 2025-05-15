namespace Hermes.HTTP.Utilities;

public static class RouteMatcher
{
    public static bool TryMatch(string template, string path, out Dictionary<string, string> parameters)
    {
        parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var templateParts = template.Trim('/').Split('/');
        var pathParts = path.Trim('/').Split('/');

        if (templateParts.Length != pathParts.Length)
            return false;

        for (int i = 0; i < templateParts.Length; i++)
        {
            var templatePart = templateParts[i];
            var pathPart = pathParts[i];

            if (templatePart.StartsWith("{") && templatePart.EndsWith("}"))
            {
                var paramName = templatePart[1..^1];
                parameters[paramName] = pathPart;
            }
            else if (!string.Equals(templatePart, pathPart, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
}
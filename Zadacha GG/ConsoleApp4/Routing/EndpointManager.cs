using System.Globalization;
using System.Reflection;
using ConsoleApp4.Attributes;
using ConsoleApp4.Controllers;

namespace ConsoleApp4.Routing;

public sealed class EndpointManager
{
    private readonly List<Endpoint> _endpoints = new();

    public void Scan()
    {
        _endpoints.Clear();

        Assembly assembly = Assembly.GetExecutingAssembly();

        IEnumerable<Type> controllers = assembly
            .GetTypes()
            .Where(type =>
                typeof(ControllerBase).IsAssignableFrom(type) &&
                type != typeof(ControllerBase) &&
                !type.IsAbstract);

        foreach (Type controller in controllers)
        {
            RouteAttribute? route = controller.GetCustomAttribute<RouteAttribute>();

            if (route is null)
                continue;

            IEnumerable<MethodInfo> actions = controller
                .GetMethods(BindingFlags.Public |
                            BindingFlags.Instance |
                            BindingFlags.DeclaredOnly)
                .Where(method =>
                    method.GetCustomAttribute<HttpGetAttribute>() is not null);

            foreach (MethodInfo action in actions)
            {
                string controllerName = GetControllerName(controller);
                string path = BuildPath(route.Pattern, controllerName, action.Name);

                bool duplicate = _endpoints.Any(endpoint =>
                    string.Equals(
                        endpoint.Path,
                        path,
                        StringComparison.OrdinalIgnoreCase));

                if (duplicate)
                {
                    throw new InvalidOperationException(
                        $"Duplicate endpoint: {path}");
                }

                _endpoints.Add(
                    new Endpoint(path, controller, action));
            }
        }
    }

    public void PrintEndpoints()
    {
        foreach (Endpoint endpoint in _endpoints.OrderBy(x => x.Path))
        {
            Console.WriteLine($"GET https://localhost/{endpoint.Path}");
        }
    }

    public void Execute(string url)
    {
        if (_endpoints.Count == 0)
        {
            throw new InvalidOperationException(
                "No endpoints are registered.");
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
        {
            throw new ArgumentException(
                "Invalid URL.",
                nameof(url));
        }

        string requestPath = uri.AbsolutePath.Trim('/');

        Endpoint? endpoint = _endpoints.FirstOrDefault(x =>
            string.Equals(
                x.Path,
                requestPath,
                StringComparison.OrdinalIgnoreCase));

        if (endpoint is null)
        {
            throw new InvalidOperationException(
                $"Endpoint '{requestPath}' not found.");
        }

        object controller =
            Activator.CreateInstance(endpoint.ControllerType)
            ?? throw new InvalidOperationException(
                "Controller could not be created.");

        Dictionary<string, string> queryValues =
            ParseQuery(uri.Query);

        object?[] arguments =
            BuildArguments(endpoint.Method, queryValues);

        endpoint.Method.Invoke(controller, arguments);
    }

    private static string GetControllerName(Type controllerType)
    {
        const string suffix = "Controller";

        if (controllerType.Name.EndsWith(
                suffix,
                StringComparison.OrdinalIgnoreCase))
        {
            return controllerType.Name[..^suffix.Length];
        }

        return controllerType.Name;
    }

    private static string BuildPath(
        string template,
        string controller,
        string action)
    {
        return template
            .Replace(
                "[controller]",
                controller,
                StringComparison.OrdinalIgnoreCase)
            .Replace(
                "[action]",
                action,
                StringComparison.OrdinalIgnoreCase)
            .Trim('/');
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var result = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);

        string cleaned = query.TrimStart('?');

        if (string.IsNullOrWhiteSpace(cleaned))
            return result;

        foreach (string item in cleaned.Split(
                     '&',
                     StringSplitOptions.RemoveEmptyEntries))
        {
            string[] pair = item.Split('=', 2);

            string key = Uri.UnescapeDataString(pair[0]);
            string value = pair.Length == 2
                ? Uri.UnescapeDataString(pair[1])
                : string.Empty;

            result[key] = value;
        }

        return result;
    }

    private static object?[] BuildArguments(
        MethodInfo method,
        Dictionary<string, string> query)
    {
        ParameterInfo[] parameters = method.GetParameters();
        object?[] values = new object?[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            ParameterInfo parameter = parameters[i];

            if (parameter.Name is null ||
                !query.TryGetValue(parameter.Name, out string? textValue))
            {
                throw new InvalidOperationException(
                    $"Missing parameter '{parameter.Name}'.");
            }

            values[i] = ConvertToType(
                textValue,
                parameter.ParameterType);
        }

        return values;
    }

    private static object? ConvertToType(
        string value,
        Type parameterType)
    {
        Type realType =
            Nullable.GetUnderlyingType(parameterType)
            ?? parameterType;

        if (realType == typeof(string))
            return value;

        if (realType == typeof(Guid))
            return Guid.Parse(value);

        if (realType.IsEnum)
            return Enum.Parse(realType, value, true);

        return Convert.ChangeType(
            value,
            realType,
            CultureInfo.InvariantCulture);
    }
}

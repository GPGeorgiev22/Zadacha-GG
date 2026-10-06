using System.Reflection;

namespace ConsoleApp4.Routing;

internal sealed class Endpoint
{
    public string Path { get; }
    public Type ControllerType { get; }
    public MethodInfo Method { get; }

    public Endpoint(string path, Type controllerType, MethodInfo method)
    {
        Path = path;
        ControllerType = controllerType;
        Method = method;
    }
}

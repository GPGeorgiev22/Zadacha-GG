namespace ConsoleApp4.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class RouteAttribute : Attribute
{
    public string Pattern { get; }

    public RouteAttribute(string pattern)
    {
        Pattern = pattern;
    }
}

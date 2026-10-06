using ConsoleApp4.Routing;

namespace ConsoleApp4;

internal class Program
{
    private static void Main(string[] args)
    {
        var endpointManager = new EndpointManager();

        endpointManager.Scan();

        Console.WriteLine("Registered GET endpoints:");
        endpointManager.PrintEndpoints();

        Console.WriteLine();

        string requestUrl = args.Length > 0
            ? args[0]
            : "https://localhost/api/Watch/PlayVideo?id=42";

        Console.WriteLine($"Executing: {requestUrl}");
        endpointManager.Execute(requestUrl);
    }
}

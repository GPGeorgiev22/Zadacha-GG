using ConsoleApp4.Attributes;

namespace ConsoleApp4.Controllers;

[Route("api/[controller]/[action]")]
public class WatchController : ControllerBase
{
    [HttpGet]
    public void PlayVideo(int id)
    {
        Console.WriteLine($"Playing video #{id}");
    }

    public void PlayAudio()
    {
        Console.WriteLine("Playing audio...");
    }
}

namespace ConsoleApp4.Controllers;

public class GameController : ControllerBase
{
    public void PlayGame()
    {
        Console.WriteLine("Game started.");
    }

    public void StopGame()
    {
        Console.WriteLine("Game stopped.");
    }
}

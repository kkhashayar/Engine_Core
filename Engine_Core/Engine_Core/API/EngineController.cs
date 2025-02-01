using Microsoft.AspNetCore.Mvc;

namespace Engine_Core.API;

[ApiController]
[Route("api/engine")]
public class EngineController : ControllerBase, IEngineServices
{
    [HttpPost("start")]
    public void StartNewGame()
    {
        // Implementation to start a new game
    }

    [HttpPost("stop")]
    public void StopGame()
    {
        // Implementation to stop the game
    }

    [HttpPost("move")]
    public string SendMove([FromBody] Move move)
    {
        // Implementation to handle move
        return "Move received";
    }
}

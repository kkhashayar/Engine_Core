using Engine_API.Interfaces;
using Engine_API.Models;

namespace Engine_API.Services;

public class EngineServices : IEngineService
{
    public Task<string> GetNewGame()
    {
        throw new NotImplementedException();
    }

    public Task<string> SendMove(Move move)
    {
        throw new NotImplementedException();
    }

    public Task<string> StopGame()
    {
        throw new NotImplementedException();
    }
}

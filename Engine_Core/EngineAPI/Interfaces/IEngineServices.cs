using Engine_API.Models;

namespace Engine_API.Interfaces;

public interface IEngineService
{
    public Task<string> GetStatus();        
    public Task<string> GetNewGame();
    public Task<string> StopGame();
    public Task<string> SendMove(Move move);
}

namespace Engine_Core.API;

public interface IEngineServices
{
    void StartNewGame();
    void StopGame();
    string SendMove(Move move);
}

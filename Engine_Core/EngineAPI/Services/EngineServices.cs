using System.Diagnostics;
using Engine_API.Interfaces;
using Engine_API.Models;

namespace Engine_API.Services
{
    public class EngineService : IEngineService
    {
        private readonly EngineHostService _engineHostService;

        public EngineService(EngineHostService engineHostService)
        {
            _engineHostService = engineHostService;
        }

        public Task<string> GetNewGame()
        {
            var engineProcess = _engineHostService.GetEngineProcess();
            if (engineProcess == null || engineProcess.HasExited)
                return Task.FromResult("Engine is not running");

            
            engineProcess.StandardInput.WriteLine("new"); //--> Winboard command
            return Task.FromResult("New game started");
        }

        public Task<string> StopGame()
        {
            var engineProcess = _engineHostService.GetEngineProcess();
            if (engineProcess == null || engineProcess.HasExited)
                return Task.FromResult("Engine is not running");

            engineProcess.StandardInput.WriteLine("stop"); //--> Winboard command
            return Task.FromResult("Game stopped");
        }

        public Task<string> SendMove(Move move)
        {
            var engineProcess = _engineHostService.GetEngineProcess();
            if (engineProcess == null || engineProcess.HasExited)
                return Task.FromResult("Engine is not running");

            engineProcess.StandardInput.WriteLine($"{move.ToString()}"); //--> Winboard command
            return Task.FromResult($"Move {move.ToString()} sent to engine");
        }

        public Task<string> GetStatus()
        {
            var engineProcess = _engineHostService.GetEngineProcess();
            if (engineProcess == null || engineProcess.HasExited)
                return Task.FromResult("Engine is not running");

            engineProcess.StandardInput.WriteLine("status");
            return Task.FromResult("Running");
        }

        public void KillEngine()
        {
            _engineHostService.KillEngine();
        }
    }
}

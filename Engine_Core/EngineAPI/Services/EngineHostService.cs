using System.Diagnostics;

namespace Engine_API.Services
{
    public class EngineHostService : BackgroundService
    {
        private readonly ILogger<EngineHostService> _logger;
        private Process? _engineProcess;

        public EngineHostService(ILogger<EngineHostService> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting Chess Engine...");

            try
            {
                if (_engineProcess == null || _engineProcess.HasExited)
                {
                    _engineProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "D:\\Data\\Repo\\K_Chess_2\\Engine_Core\\Engine_UI\\bin\\Debug\\net8.0\\Engine_UI.exe",
                            RedirectStandardInput = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    bool started = _engineProcess.Start();
                    _logger.LogInformation($"Engine started: {started}");

                    // Read output continuously
                    _engineProcess.OutputDataReceived += (sender, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                        {
                            _logger.LogInformation($"Engine Output: {args.Data}");
                        }
                    };

                    _engineProcess.ErrorDataReceived += (sender, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                        {
                            _logger.LogError($"Engine Error: {args.Data}");
                        }
                    };

                    _engineProcess.BeginOutputReadLine();
                    _engineProcess.BeginErrorReadLine();

                    // Keep the process running and monitor it
                    await Task.Run(() => _engineProcess.WaitForExit(), stoppingToken);

                    if (_engineProcess.HasExited)
                    {
                        _logger.LogWarning("Engine process has exited unexpectedly.");
                    }
                }
                else
                {
                    _logger.LogWarning("Engine process was already running.");
                }

                if (_engineProcess != null)
                    _logger.LogInformation($"Process ID: {_engineProcess.Id}, HasExited: {_engineProcess.HasExited}");

            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to start engine: {ex.Message}");
            }
        }



        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Stopping Chess Engine...");
            if (_engineProcess != null && !_engineProcess.HasExited)
            {
                _engineProcess.Kill();
                await _engineProcess.WaitForExitAsync(stoppingToken);
            }
        }

        public Process? GetEngineProcess()
        {
            if (_engineProcess == null)
            {
                _logger.LogWarning("GetEngineProcess: Engine process is NULL.");
                return null;
            }

            if (_engineProcess.HasExited)
            {
                _logger.LogWarning("GetEngineProcess: Engine process has exited.");
                return null;
            }

            _logger.LogInformation("GetEngineProcess: Returning active engine process.");
            return _engineProcess;
        }

    }
}

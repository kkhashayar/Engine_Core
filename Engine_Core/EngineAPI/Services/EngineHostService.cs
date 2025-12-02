using System.Diagnostics;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq; // Added for LINQ

namespace Engine_API.Services
{
    public interface IEngineHostService
    {
        Task<string> SendCommandAndWaitForResponse(string command, string expectedPrefix);
        Process? GetEngineProcess();
        void KillEngine();
    }

    public class EngineHostService : BackgroundService, IEngineHostService
    {
        private readonly ILogger<EngineHostService> _logger;
        private Process? _engineProcess;
        private StreamWriter? _inputWriter;

        private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingResponses =
            new ConcurrentDictionary<string, TaskCompletionSource<string>>();

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
                    string engineExeName = "Engine_UI.exe";

                    // --- STRATEGY 3: DEEP SEARCH ---
                    // 1. Find the "Engine_Core" root folder
                    string rootSearchPath = FindSolutionRoot(AppContext.BaseDirectory);
                    _logger.LogInformation($"Searching for '{engineExeName}' recursively starting from: {rootSearchPath}");

                    // 2. Scan ALL subdirectories for the executable
                    string enginePath = Directory.GetFiles(rootSearchPath, engineExeName, SearchOption.AllDirectories)
                                                 .FirstOrDefault(path => path.Contains("bin") && path.Contains("Debug")); // Prefer Debug build

                    // If not found in Debug, take ANY match
                    if (string.IsNullOrEmpty(enginePath))
                    {
                        enginePath = Directory.GetFiles(rootSearchPath, engineExeName, SearchOption.AllDirectories).FirstOrDefault();
                    }

                    if (string.IsNullOrEmpty(enginePath))
                    {
                        _logger.LogCritical($"CRITICAL: '{engineExeName}' was NOT found anywhere inside '{rootSearchPath}'.");
                        _logger.LogCritical($"ACTION REQUIRED: Right-click 'Engine_UI' project -> Rebuild.");
                        return;
                    }

                    _logger.LogInformation($"FOUND ENGINE AT: {enginePath}");

                    _engineProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = enginePath,
                            RedirectStandardInput = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    bool started = _engineProcess.Start();
                    _logger.LogInformation($"Engine started: {started}");

                    if (started)
                    {
                        _inputWriter = _engineProcess.StandardInput;
                    }

                    _engineProcess.OutputDataReceived += (sender, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                        {
                            _logger.LogInformation($"Engine Output: {args.Data}");
                            ProcessEngineOutput(args.Data);
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

                    await Task.Run(() => _engineProcess.WaitForExit(), stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to start engine: {ex.Message}");
            }
        }

        // Helper to find the common root folder (Engine_Core)
        private string FindSolutionRoot(string startPath)
        {
            DirectoryInfo? dir = new DirectoryInfo(startPath);
            while (dir != null)
            {
                // If we see "Engine_Core" or "Engine_UI" as a folder name, we are likely near the root
                if (dir.Name == "Engine_Core" || dir.GetDirectories("Engine_UI").Any())
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            // Fallback: just return the drive root or the start path to limit damage, 
            // but realistically we should stop at the project repo root.
            return Path.GetFullPath(Path.Combine(startPath, @"..\..\..\.."));
        }

        public async Task<string> SendCommandAndWaitForResponse(string command, string expectedPrefix)
        {
            if (_engineProcess == null || _engineProcess.HasExited || _inputWriter == null)
            {
                return "Error: Engine not running.";
            }

            var tcs = new TaskCompletionSource<string>();
            if (!_pendingResponses.TryAdd(command, tcs))
            {
                _pendingResponses.TryRemove(command, out _);
                _pendingResponses.TryAdd(command, tcs);
            }

            try
            {
                await _inputWriter.WriteLineAsync(command);
                _logger.LogInformation($"Sent command: {command}");

                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(60));
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    _pendingResponses.TryRemove(command, out _);
                    return "Error: Command timed out.";
                }

                return await tcs.Task;
            }
            catch (Exception ex)
            {
                _pendingResponses.TryRemove(command, out _);
                return $"Error: {ex.Message}";
            }
        }

        private void ProcessEngineOutput(string output)
        {
            if (output.StartsWith("move ") && _pendingResponses.TryRemove("go", out var tcs))
            {
                tcs.SetResult(output.Replace("move ", "").Trim());
            }
            else if (output.Contains("# New game started") && _pendingResponses.TryRemove("new", out var newTcs))
            {
                newTcs.SetResult("OK");
            }
            else if (output.Contains("# Status command acknowledged") && _pendingResponses.TryRemove("status", out var statusTcs))
            {
                statusTcs.SetResult("OK");
            }
            else if (output.StartsWith("bestmove ") && _pendingResponses.TryRemove("go", out var bestMoveTcs))
            {
                bestMoveTcs.SetResult(output.Replace("bestmove ", "").Trim());
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            if (_engineProcess != null && !_engineProcess.HasExited)
            {
                try { await _inputWriter?.WriteLineAsync("quit")!; } catch { /* ignore */ }
                _engineProcess.Kill();
                await _engineProcess.WaitForExitAsync(stoppingToken);
            }
        }

        public Process? GetEngineProcess() => _engineProcess;

        public void KillEngine()
        {
            if (_engineProcess != null && !_engineProcess.HasExited)
            {
                _engineProcess.Kill();
                _engineProcess.WaitForExit();
                _engineProcess.Dispose();
                _engineProcess = null;
            }
        }
    }
}
using System.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace Engine_API.Services;

public class EngineHostService : BackgroundService
{
    private Process? _engineProcess;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() =>
                {
                    _engineProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "D:\\Data\\Repo\\K_Chess_2\\Engine_Core\\Engine_UI\\Program.cs",
                            Arguments = "run",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    _engineProcess.Start();
                    _engineProcess.WaitForExit();
                });
    }


    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _engineProcess?.Kill();
        await base.StopAsync(cancellationToken);    
    }
}

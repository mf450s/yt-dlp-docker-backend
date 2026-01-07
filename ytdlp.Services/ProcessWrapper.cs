using System.Diagnostics;
using ytdlp.Services.Interfaces;

namespace ytdlp.Services;

/// <summary>
/// Wrapper around System.Diagnostics.Process for dependency injection and testability.
/// </summary>
public class ProcessWrapper : IProcess
{
    private readonly Process _process;

    public ProcessWrapper()
    {
        _process = new Process();
    }

    public ProcessStartInfo StartInfo
    {
        get => _process.StartInfo;
        set => _process.StartInfo = value;
    }

    public TextReader StandardOutput => _process.StandardOutput;
    public TextReader StandardError => _process.StandardError;
    public int ExitCode => _process.ExitCode;

    public bool Start() => _process.Start();

    public Task WaitForExitAsync(CancellationToken cancellationToken = default)
        => _process.WaitForExitAsync(cancellationToken);

    public void Dispose()
    {
        _process?.Dispose();
        GC.SuppressFinalize(this);
    }
}

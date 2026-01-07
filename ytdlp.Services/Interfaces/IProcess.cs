using System.Diagnostics;

namespace ytdlp.Services.Interfaces
{
    public interface IProcess : IDisposable
    {
        ProcessStartInfo StartInfo { get; set; }
        TextReader StandardOutput { get; }
        TextReader StandardError { get; }
        int ExitCode { get; }
        bool Start();
        Task WaitForExitAsync(CancellationToken cancellationToken = default);
    }
}

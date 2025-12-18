using ytdlp.Services.Interfaces;

namespace ytdlp.Services
{

    // ProcessFactory.cs - Factory erstellt ProcessWrapper
    public class ProcessFactory : IProcessFactory
    {
        public IProcess CreateProcess() => new ProcessWrapper();
    }
}

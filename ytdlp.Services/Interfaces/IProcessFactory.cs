using System;

namespace ytdlp.Services.Interfaces;

public interface IProcessFactory
{
    IProcess CreateProcess();
}

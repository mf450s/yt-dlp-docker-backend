using System;

namespace ytdlp.Services.Interfaces;

public interface IPathParserService
{
    string CheckAndFixPaths(string line);
}

using System;

namespace ytdlp.Services.Interfaces;

public interface IPathParserService
{
    string CheckAndFixPaths(string line);
    
    /// <summary>
    /// Gets the cookies folder path from the PathConfiguration.
    /// </summary>
    /// <returns>The full path to the cookies folder.</returns>
}

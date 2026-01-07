using System.Diagnostics.CodeAnalysis;
using ytdlp.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace ytdlp.Tests.Services;

[ExcludeFromCodeCoverage]
public class PathParserServiceTests
{
    #region Setup
    public PathParserService GetPathParserService(
        string downloads = "/app/downloads/",
        string archive = "/app/archive/",
        string cookies = "/app/cookies"
    )
    {
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Paths:Downloads"]).Returns(downloads);
        configMock.Setup(c => c["Paths:Archive"]).Returns(archive);
        configMock.Setup(c => c["Paths:Cookies"]).Returns(cookies);

        var loggerMock = new Mock<ILogger<PathParserService>>();
        
        return new PathParserService(configMock.Object, loggerMock.Object);
    }
    #endregion

    [Theory]
    // Download-Pfad Tests
    [InlineData("-o /app/downloads/video", "-o \"/app/downloads/video\"")]
    [InlineData("-o video/", "-o \"/app/downloads/video/\"")]
    [InlineData("-o /video/", "-o \"/app/downloads/video/\"")]
    [InlineData("-o video", "-o \"/app/downloads/video\"")]
    [InlineData("-o \"video xyz\"", "-o \"/app/downloads/video xyz\"")]
    [InlineData("-o \"%(title)s.%(ext)s\"", "-o \"/app/downloads/%(title)s.%(ext)s\"")]
    [InlineData("-o \" spaced \"", "-o \"/app/downloads/spaced\"")]
    // Archive-Pfad Tests
    [InlineData("--download-archive /app/archive/video.txt", "--download-archive \"/app/archive/video.txt\"")]
    [InlineData("--download-archive video.txt", "--download-archive \"/app/archive/video.txt\"")]
    [InlineData("--download-archive /video.txt", "--download-archive \"/app/archive/video.txt\"")]
    [InlineData("--download-archive \"archive.txt\"", "--download-archive \"/app/archive/archive.txt\"")]
    // Cookie-Pfad Tests
    [InlineData("--cookies /app/cookies/cookies.txt", "--cookies \"/app/cookies/cookies.txt\"")]
    [InlineData("--cookies cookies.txt", "--cookies \"/app/cookies/cookies.txt\"")]
    [InlineData("--cookies /cookies.txt", "--cookies \"/app/cookies/cookies.txt\"")]
    [InlineData("--cookies \"my cookies.txt\"", "--cookies \"/app/cookies/my cookies.txt\"")]
    [InlineData("--cookies youtube-cookies.txt", "--cookies \"/app/cookies/youtube-cookies.txt\"")]
    [InlineData("--cookies \"  cookies.txt  \"", "--cookies \"/app/cookies/cookies.txt\"")]
    // Keine Änderungen nötig
    [InlineData("--format bestvideo", "--format bestvideo")]
    [InlineData("-f best", "-f best")]
    [InlineData("# comment", "# comment")]
    [InlineData("--cookies-from-browser chrome", "--cookies-from-browser chrome")]
    public void CheckAndFixPaths_ReturnsCorrectLine(string inputLine, string expectedOutputLine)
    {
        // Arrange
        var service = GetPathParserService();

        // Act
        string actualOutputLine = service.CheckAndFixPaths(inputLine);

        // Assert
        Assert.Equal(expectedOutputLine, actualOutputLine);
    }
}

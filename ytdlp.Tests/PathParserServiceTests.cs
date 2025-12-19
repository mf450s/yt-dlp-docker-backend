using System.Diagnostics.CodeAnalysis;
using ytdlp.Services;
using Microsoft.Extensions.Options;
using ytdlp.Configs;
using FluentAssertions;
namespace ytdlp.Tests.Services;

[ExcludeFromCodeCoverage]
public class PathParserServiceTests
{
    #region Helper
    public PathParserSerivce GetConfigsServices(
        IOptions<PathConfiguration>? iOptionsPathConfig = null,
        PathConfiguration? pathConfiguration = null
    )
    {
        iOptionsPathConfig ??= pathConfiguration != null
            ? Options.Create(pathConfiguration)
            : Options.Create(paths);

        return new PathParserSerivce(iOptionsPathConfig);
    }
    private readonly PathConfiguration paths = new();


    #endregion

    #region CheckAndFixOutputAndPath
    [Theory]
    [InlineData("-o video.mp4")]
    [InlineData("--output video.mp4")]
    public void CheckAndFixOutputAndPath_WithOutputOption_ShouldCallFixOutputPath(string input)
    {
        // Arrange
        var sut = GetConfigsServices();

        // Act
        var result = sut.CheckAndFixPaths(input);

        // Assert
        result.Should().Contain(paths.Downloads);
        result.Should().Contain("video.mp4");
    }

    [Theory]
    [InlineData("-P /downloads")]
    [InlineData("--paths home:/downloads")]
    public void CheckAndFixOutputAndPath_WithPathOption_ShouldCallFixPathPath(string input)
    {
        // Arrange
        var sut = GetConfigsServices();

        // Act
        var result = sut.CheckAndFixPaths(input);

        // Assert
        result.Should().Contain(paths.Downloads);
    }

    [Theory]
    [InlineData("--format bestvideo")]
    [InlineData("-f best")]
    [InlineData("# comment")]
    public void CheckAndFixOutputAndPath_WithoutOutputOrPath_ShouldReturnUnchanged(string input)
    {
        // Arrange
        var sut = GetConfigsServices();

        // Act
        var result = sut.CheckAndFixPaths(input);

        // Assert
        result.Should().Be(input);
    }
    #endregion
    #region FixOutputPath
    [Theory]
    [InlineData("-o video.mp4", "-o \"")]
    [InlineData("--output \"video.mp4\"", "--output \"")]
    [InlineData("-o \"%(title)s.%(ext)s\"", "-o \"")]
    public void FixOutputPath_ShouldAddDownloadsFolderAndQuotes(string input, string expectedStart)
    {
        // Arrange
        var sut = GetConfigsServices();

        // Act
        var result = sut.FixOutputPath(input);

        // Assert
        result.Should().StartWith(expectedStart);
        result.Should().Contain(paths.Downloads);
        result.Should().EndWith("\"");
    }

    [Fact]
    public void FixOutputPath_WhenConfigFolderAlreadyPresent_ShouldNotDuplicate()
    {
        // Arrange
        var sut = GetConfigsServices();
        var input = $"-o {Path.Combine(paths.Config, "video.mp4")}";

        // Act
        var result = sut.FixOutputPath(input);

        // Assert
        result.Should().Contain(paths.Config);
        var occurrences = result.Split(new[] { paths.Config }, StringSplitOptions.None).Length - 1;
        occurrences.Should().Be(1, "ConfigFolder should only appear once");
    }

    [Fact]
    public void FixOutputPath_WithComplexTemplate_ShouldPreserveTemplate()
    {
        // Arrange
        var sut = GetConfigsServices();
        var input = "-o \"%(title)s.%(ext)s\"";

        // Act
        var result = sut.FixOutputPath(input);

        // Assert
        result.Should().Contain("%(title)s.%(ext)s");
        result.Should().Contain(paths.Downloads);
    }
    #endregion
    #region FixPathPath
    [Theory]
    [InlineData("-P /downloads")]
    [InlineData("--paths home:/downloads")]
    [InlineData("--paths \"temp:/files\"")]
    public void FixPathPath_ShouldAddDownloadFolder(string input)
    {
        // Arrange & Act
        var sut = GetConfigsServices();
        var result = sut.FixPathPath(input);

        // Assert
        result.Should().Contain(paths.Downloads);
    }

    [Fact]
    public void FixPathPath_WhenDownloadFolderAlreadyPresent_ShouldReturnUnchanged()
    {
        // Bleibt als separater Test
    }

    #endregion
    #region FixArchivePath
    [Theory]
    [InlineData($"--download-archive /app/archive/download", "--download-archive \"/app/archive/download\"")]
    [InlineData($"--download-archive /download", "--download-archive \"/app/archive/download\"")]

    public void FixConfigPath_With(string input, string expectedOutput)
    {
        // Act
        var sut = GetConfigsServices();

        // Assert
        var result = sut.FixArchivePath(input);

        result.Should().Be(expectedOutput);
    }

    #endregion
}
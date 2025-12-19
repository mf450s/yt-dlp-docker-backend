using System.Diagnostics.CodeAnalysis;
using ytdlp.Services;
using System.IO.Abstractions.TestingHelpers;
using Microsoft.Extensions.Options;
using ytdlp.Configs;
using FluentAssertions;
using FluentResults;
using Microsoft.VisualBasic;

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
    [Fact]
    public void FixOutputPath_ShouldAddConfigFolderIfMissing()
    {
        // Arrange
        var sut = GetConfigsServices();
        var input = "-o video.mp4";

        // Act
        var result = sut.FixOutputPath(input);

        // Assert
        result.Should().StartWith("-o \"");
        result.Should().Contain(paths.Downloads);
        result.Should().Contain("video.mp4");
        result.Should().EndWith("\"");
    }

    [Fact]
    public void FixOutputPath_WithExistingQuotes_ShouldRemoveAndReaddQuotes()
    {
        // Arrange
        var sut = GetConfigsServices();
        var input = "--output \"video.mp4\"";

        // Act
        var result = sut.FixOutputPath(input);

        // Assert
        result.Should().StartWith("--output \"");
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
    [Fact]
    public void FixPathPath_WithSimplePath_ShouldAddDownloadFolder()
    {
        // Arrange
        var sut = GetConfigsServices();
        var input = "-P /downloads";

        // Act
        var result = sut.FixPathPath(input);

        // Assert
        result.Should().StartWith("-P \"");
        result.Should().Contain(paths.Downloads);
        result.Should().EndWith("\"");
    }

    [Fact]
    public void FixPathPath_WithTypeAndPath_ShouldAddDownloadFolderToPath()
    {
        // Arrange
        var sut = GetConfigsServices();
        var input = "--paths home:/downloads";

        // Act
        var result = sut.FixPathPath(input);

        // Assert
        result.Should().StartWith("--paths \"home:");
        result.Should().Contain(paths.Downloads);
        result.Should().Contain("downloads");
        result.Should().EndWith("\"");
    }

    [Fact]
    public void FixPathPath_WhenDownloadFolderAlreadyPresent_ShouldReturnUnchanged()
    {
        // Arrange
        var sut = GetConfigsServices();
        var input = $"-P \"{paths.Downloads}/downloads\"";

        // Act
        var result = sut.FixPathPath(input);

        // Assert
        result.Should().Be(input);
    }

    [Fact]
    public void FixPathPath_WithQuotedPath_ShouldHandleCorrectly()
    {
        // Arrange
        var sut = GetConfigsServices();
        var input = "--paths \"temp:/files\"";

        // Act
        var result = sut.FixPathPath(input);

        // Assert
        result.Should().Contain("temp:");
        result.Should().Contain(paths.Downloads);
    }
    #endregion
}
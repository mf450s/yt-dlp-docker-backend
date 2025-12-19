using System.IO.Abstractions.TestingHelpers;
using Microsoft.Extensions.Options;
using ytdlp.Configs;
using Moq;
using FluentAssertions;
using ytdlp.Services;
using FluentResults;
using System.Diagnostics.CodeAnalysis;

namespace ytdlp.Tests.Services;

/// <summary>
/// Unit tests for ConfigsServices class covering configuration file operations.
/// </summary>
[ExcludeFromCodeCoverage]
public class ConfigsServicesTests
{
    #region Test Data Constants

    /// <summary>
    /// Static test configuration constants for reusable test data.
    /// </summary>
    private static class TestConfigContent
    {
        public const string Music = "# Music Config\n--extract-audio\n--audio-format mp3";
        public const string Video = "# Video Config\n--format bestvideo";
        public const string Playlist = "# Playlist Config\n--yes-playlist";
        public const string Multiline = "# Config Header\n--format bestvideo\n--output '%(title)s'\n# Comment";
        public const string Empty = "";
        public const string CommentOnly = "# This is a comment\n# Another comment\n#Third comment";
        public const string Complex = "# Download config\n--format bestvideo\n# Output settings\n-o video.mp4";
    }

    #endregion

    #region Helper Methods

    private ConfigsServices GetConfigsServices(
        MockFileSystem? mockFileSystem = null,
        IOptions<PathConfiguration>? iOptionsPathConfig = null,
        PathConfiguration? pathConfiguration = null,
        PathParserSerivce? pathParserSerivce = null)
    {
        mockFileSystem ??= new MockFileSystem();
        iOptionsPathConfig ??= pathConfiguration != null
            ? Options.Create(pathConfiguration)
            : Options.Create(paths);
        pathParserSerivce ??= new PathParserSerivce(iOptionsPathConfig);

        return new ConfigsServices(mockFileSystem, iOptionsPathConfig, pathParserSerivce);
    }

    private MockFileSystem CreateMockFileSystemWithConfigs(PathConfiguration paths, Dictionary<string, string> configs)
    {
        var fileData = configs.ToDictionary(
            kvp => $"{paths.Config}{kvp.Key}.conf",
            kvp => new MockFileData(kvp.Value) as MockFileData
        );
        return new MockFileSystem(fileData);
    }

    private readonly PathConfiguration paths = new();

    #endregion

    #region GetWholeConfigPath
    [Theory]
    [InlineData("testconfig")]
    [InlineData("music")]
    [InlineData("video")]
    [InlineData("playlist")]
    public void GetWholeConfigPath_WithValidName_ShouldReturnCorrectPath(string configName)
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var sut = GetConfigsServices(mockFileSystem);
        var expectedPath = $"{paths.Config}{configName}.conf"; // paths statt _paths

        // Act
        var result = sut.GetWholeConfigPath(configName);

        // Assert
        result.Should().Be(expectedPath);
    }

    #endregion

    #region GetAllConfigNames

    [Fact]
    public void GetAllConfigNames_WhenMultipleConfigsExist_ShouldReturnAllConfigNames()
    {
        // Arrange
        var configs = new Dictionary<string, string>
        {
            { "music", TestConfigContent.Music },
            { "video", TestConfigContent.Video },
            { "playlist", TestConfigContent.Playlist }
        };
        var mockFileSystem = CreateMockFileSystemWithConfigs(paths, configs);
        var sut = GetConfigsServices(mockFileSystem);

        // Act
        var result = sut.GetAllConfigNames();

        // Assert
        result.Should().HaveCount(3)
            .And.Contain(["music", "video", "playlist"]);
    }

    [Fact]
    public void GetAllConfigNames_WhenDirectoryIsEmpty_ShouldReturnEmptyList()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory(paths.Config);
        var sut = GetConfigsServices(mockFileSystem);

        // Act
        var result = sut.GetAllConfigNames();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetAllConfigNames_WhenMixedFileTypesExist_ShouldReturnOnlyConfFiles()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { $"{paths.Config}music.conf", new MockFileData(TestConfigContent.Music) },
            { $"{paths.Config}readme.txt", new MockFileData("") },
            { $"{paths.Config}video.conf", new MockFileData(TestConfigContent.Video) },
            { $"{paths.Config}backup.bak", new MockFileData("") }
        });
        var sut = GetConfigsServices(mockFileSystem);

        // Act
        var result = sut.GetAllConfigNames();

        // Assert
        result.Should().HaveCount(2)
            .And.Contain(["music", "video"])
            .And.NotContain(["readme", "backup"]);
    }

    [Fact]
    public void GetAllConfigNames_ShouldReturnNamesWithoutExtension()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { $"{paths.Config}test.conf", new MockFileData("") }
        });
        var sut = GetConfigsServices(mockFileSystem);

        // Act
        var result = sut.GetAllConfigNames();

        // Assert
        result.Should().Contain("test")
            .And.NotContain("test.conf");
    }

    #endregion

    #region GetConfigContent
    [Theory]
    [InlineData("music", TestConfigContent.Music)]
    [InlineData("video", TestConfigContent.Video)]
    [InlineData("playlist", TestConfigContent.Playlist)]
    [InlineData("empty", TestConfigContent.Empty)]
    public void GetConfigContentByName_WhenFileExists_ShouldReturnContentSuccessfully(
        string configName, string expectedContent)
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
    {
        { $"{paths.Config}{configName}.conf", new MockFileData(expectedContent) }
    });
        var sut = GetConfigsServices(mockFileSystem);

        // Act
        var result = sut.GetConfigContentByName(configName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedContent);
    }

    [Fact]
    public void GetConfigContentByName_WhenFileDoesNotExist_ShouldReturnFailure()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory(paths.Config);
        var sut = GetConfigsServices(mockFileSystem);

        // Act
        var result = sut.GetConfigContentByName("nonexistent");

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Message.Should()
            .Contain("Config file not found")
            .And.Contain($"{paths.Config}nonexistent.conf");
    }
    #endregion

    #region DeleteConfig
    [Theory]
    [InlineData("music")]
    [InlineData("video")]
    [InlineData("playlist")]
    public void DeleteConfigByName_WhenFileExists_ShouldDeleteOnlySpecifiedFile(string configName)
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
    {
        { $"{paths.Config}{configName}.conf", new MockFileData("# Config") },
        { $"{paths.Config}other.conf", new MockFileData("# Other") }
    });
        var sut = GetConfigsServices(mockFileSystem);
        var targetPath = $"{paths.Config}{configName}.conf";

        // Act
        sut.DeleteConfigByName(configName);

        // Assert
        mockFileSystem.File.Exists(targetPath).Should().BeFalse("target file should be deleted");
        mockFileSystem.File.Exists($"{paths.Config}other.conf").Should().BeTrue("other files should remain");
    }

    [Fact]
    public void DeleteConfigByName_WhenFileDoesNotExist_ShouldNotThrowException()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory(paths.Config);
        var sut = GetConfigsServices(mockFileSystem);

        // Act
        var act = () => sut.DeleteConfigByName("nonexistent");

        // Assert
        act.Should().NotThrow("deleting non-existent file should not throw");
    }

    #endregion

    #region CreateConfig

    [Theory]
    [InlineData("music", TestConfigContent.Music)]
    [InlineData("video", TestConfigContent.Video)]
    [InlineData("playlist", TestConfigContent.Playlist)]
    [InlineData("empty", TestConfigContent.Empty)]
    [InlineData("multiline", TestConfigContent.Multiline)]
    public async Task CreateNewConfigAsync_WhenFileDoesNotExist_ShouldCreateFileCorrectly(
        string configName, string configContent)
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory(paths.Config);
        var sut = GetConfigsServices(mockFileSystem);
        var filePath = $"{paths.Config}{configName}.conf";

        // Act
        var result = await sut.CreateNewConfigAsync(configName, configContent);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("created successfully").And.Contain(configName);
        mockFileSystem.File.Exists(filePath).Should().BeTrue();
        mockFileSystem.File.ReadAllText(filePath).Should().Be(configContent);
    }

    [Theory]
    [InlineData("existing")]
    public async Task CreateNewConfigAsync_WhenFileAlreadyExists_ShouldReturnFailure(string configName)
    {
        // Arrange
        var originalContent = "# Original Config";
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
    {
        { $"{paths.Config}{configName}.conf", new MockFileData(originalContent) }
    });
        var sut = GetConfigsServices(mockFileSystem);
        var filePath = $"{paths.Config}{configName}.conf";

        // Act
        var result = await sut.CreateNewConfigAsync(configName, "# New Content");

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Message.Should().Contain("already exists");
        mockFileSystem.File.Exists(filePath).Should().BeTrue();
        mockFileSystem.File.ReadAllText(filePath).Should().Be(originalContent);
    }

    #endregion

    #region SetConfigContent

    [Theory]
    [InlineData("music", TestConfigContent.Music)]
    [InlineData("video", TestConfigContent.Video)]
    [InlineData("playlist", TestConfigContent.Playlist)]
    [InlineData("test", TestConfigContent.Empty)]
    public async Task SetConfigContentAsync_WhenFileExists_ShouldUpdateCorrectly(
        string configName, string newContent)
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
    {
        { $"{paths.Config}{configName}.conf", new MockFileData("# Old Content") }
    });
        var sut = GetConfigsServices(mockFileSystem);
        var filePath = $"{paths.Config}{configName}.conf";

        // Act
        var result = await sut.SetConfigContentAsync(configName, newContent);

        // Assert
        result.IsSuccess.Should().BeTrue();
        mockFileSystem.File.Exists(filePath).Should().BeTrue();
        mockFileSystem.File.ReadAllText(filePath).Should().Be(newContent);
    }

    [Fact]
    public async Task SetConfigContentAsync_WhenFileDoesNotExist_ShouldReturnFailure()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory(paths.Config);
        var sut = GetConfigsServices(mockFileSystem);

        // Act
        var result = await sut.SetConfigContentAsync("nonexistent", "# New Content");

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should()
            .Contain("doesnt exists")
            .And.Contain("nonexistent");
        mockFileSystem.File.Exists($"{paths.Config}nonexistent.conf").Should().BeFalse();
    }

    #endregion

    #region SplitArguments

    [Theory]
    [InlineData("-o \"file.mp4\" -f best", new[] { "-o \"file.mp4\"", "-f best" })]
    [InlineData("--output '%(title)s.%(ext)s'", new[] { "--output '%(title)s.%(ext)s'" })]
    [InlineData("-f best -o output.mp4", new[] { "-f best", "-o output.mp4" })]
    [InlineData("--format bestvideo", new[] { "--format bestvideo" })]
    [InlineData("   ", new string[0])]
    public void SplitArguments_WithVariousFormats_ShouldSplitCorrectly(string input, string[] expected)
    {
        // Arrange
        var sut = GetConfigsServices();

        // Act
        var result = ConfigsServices.SplitArguments(input);

        // Assert
        result.Should().Equal(expected);
    }

    [Fact]
    public void SplitArguments_WithEmptyString_ShouldReturnEmptyList()
    {
        // Arrange
        var sut = GetConfigsServices();

        // Act
        var result = ConfigsServices.SplitArguments(string.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region FixConfigContent

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void FixConfigContent_WithEmptyOrWhitespace_ShouldHandleGracefully(string content)
    {
        // Arrange
        var sut = GetConfigsServices();

        // Act
        var result = sut.FixConfigContent(content);

        // Assert
        result.Should().NotBeNull();
    }

    [Theory]
    [InlineData(TestConfigContent.CommentOnly)]
    [InlineData("# Comment\nsome config line\n# Another comment")]
    [InlineData(TestConfigContent.Complex)]
    public void FixConfigContent_WithComments_ShouldPreserveAllComments(string content)
    {
        // Arrange
        var sut = GetConfigsServices();

        // Act
        var result = sut.FixConfigContent(content);

        // Assert
        result.Should().Contain("#");
    }

    [Theory]
    [InlineData("config_value", "/fixed/path/config_value")]
    [InlineData("   config   ", "fixed_config")]
    [InlineData("--format bestvideo\n-o output.mp4", null, true)]
    [InlineData(@"--format bestvideo
-o video.mp4
--paths home:/downloads
# End of config", null, true)]
    public void FixConfigContent_WithConfigLines_ShouldCallPathParser(
        string input, string? expectedResult, bool? expectDownloads = null)
    {
        // Arrange
        var mockPathParser = new Mock<PathParserSerivce>();
        mockPathParser.Setup(p => p.CheckAndFixPaths(It.IsAny<string>()))
            .Returns<string>(s => $"/fixed/path/{s.Trim()}");

        var sut = GetConfigsServices(pathParserSerivce: mockPathParser.Object);

        // Act
        var result = sut.FixConfigContent(input);

        // Assert
        mockPathParser.Verify(p => p.CheckAndFixPaths(It.IsAny<string>()), Times.AtLeastOnce());
        if (expectedResult != null) result.Should().Contain(expectedResult);
        if (expectDownloads == true) result.Should().Contain(paths.Downloads);
    }

    [Fact]
    public void FixConfigContent_WithNullPathParser_ShouldThrowNullReferenceException()
    {
        // Arrange
        var sut = GetConfigsServices(pathParserSerivce: null!);

        // Act
        var act = () => sut.FixConfigContent("some content");

        // Assert
        act.Should().Throw<NullReferenceException>();
    }

    #endregion
}
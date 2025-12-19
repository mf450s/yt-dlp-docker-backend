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
            : Options.Create(_paths);
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

    private readonly PathConfiguration _paths = new();

    #endregion

    #region GetWholeConfigPath

    [Fact]
    public void GetWholeConfigPath_WithValidName_ShouldReturnCorrectPath()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var sut = GetConfigsServices(mockFileSystem);
        const string configName = "testconfig";

        // Act
        var result = sut.GetWholeConfigPath(configName);

        // Assert
        result.Should().Be($"{_paths.Config}{configName}.conf");
    }

    [Theory]
    [InlineData("music")]
    [InlineData("video")]
    [InlineData("playlist")]
    public void GetWholeConfigPath_WithDifferentNames_ShouldReturnCorrectPath(string configName)
    {
        // Arrange
        var sut = GetConfigsServices();

        // Act
        var result = sut.GetWholeConfigPath(configName);

        // Assert
        result.Should().Be($"{_paths.Config}{configName}.conf");
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
        var mockFileSystem = CreateMockFileSystemWithConfigs(_paths, configs);
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
        mockFileSystem.AddDirectory(_paths.Config);
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
            { $"{_paths.Config}music.conf", new MockFileData(TestConfigContent.Music) },
            { $"{_paths.Config}readme.txt", new MockFileData("") },
            { $"{_paths.Config}video.conf", new MockFileData(TestConfigContent.Video) },
            { $"{_paths.Config}backup.bak", new MockFileData("") }
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
            { $"{_paths.Config}test.conf", new MockFileData("") }
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

    [Fact]
    public void GetConfigContentByName_WhenFileExists_ShouldReturnContentSuccessfully()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { $"{_paths.Config}music.conf", new MockFileData(TestConfigContent.Music) }
        });
        var sut = GetConfigsServices(mockFileSystem);

        // Act
        var result = sut.GetConfigContentByName("music");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(TestConfigContent.Music);
    }

    [Fact]
    public void GetConfigContentByName_WhenFileDoesNotExist_ShouldReturnFailure()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory(_paths.Config);
        var sut = GetConfigsServices(mockFileSystem);

        // Act
        var result = sut.GetConfigContentByName("nonexistent");

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Message.Should()
            .Contain("Config file not found")
            .And.Contain($"{_paths.Config}nonexistent.conf");
    }

    [Theory]
    [InlineData("music", TestConfigContent.Music)]
    [InlineData("video", TestConfigContent.Video)]
    [InlineData("playlist", TestConfigContent.Playlist)]
    public void GetConfigContentByName_WithDifferentConfigs_ShouldReturnCorrectContent(string configName, string expectedContent)
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { $"{_paths.Config}{configName}.conf", new MockFileData(expectedContent) }
        });
        var sut = GetConfigsServices(mockFileSystem);

        // Act
        var result = sut.GetConfigContentByName(configName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedContent);
    }

    [Fact]
    public void GetConfigContentByName_WhenFileIsEmpty_ShouldReturnEmptyStringSuccessfully()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { $"{_paths.Config}empty.conf", new MockFileData(TestConfigContent.Empty) }
        });
        var sut = GetConfigsServices(mockFileSystem);

        // Act
        var result = sut.GetConfigContentByName("empty");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    #endregion

    #region DeleteConfig

    [Fact]
    public void DeleteConfigByName_WhenFileExists_ShouldDeleteFile()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { $"{_paths.Config}music.conf", new MockFileData("# Music Config") }
        });
        var sut = GetConfigsServices(mockFileSystem);
        var filePath = $"{_paths.Config}music.conf";

        // Act
        sut.DeleteConfigByName("music");

        // Assert
        mockFileSystem.File.Exists(filePath).Should().BeFalse("file should be deleted");
    }

    [Theory]
    [InlineData("music")]
    [InlineData("video")]
    [InlineData("playlist")]
    public void DeleteConfigByName_WithDifferentNames_ShouldDeleteOnlySpecifiedFile(string configName)
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { $"{_paths.Config}{configName}.conf", new MockFileData("# Config Content") },
            { $"{_paths.Config}other.conf", new MockFileData("# Other Config") }
        });
        var sut = GetConfigsServices(mockFileSystem);

        // Act
        sut.DeleteConfigByName(configName);

        // Assert
        mockFileSystem.File.Exists($"{_paths.Config}{configName}.conf").Should().BeFalse("specified file should be deleted");
        mockFileSystem.File.Exists($"{_paths.Config}other.conf").Should().BeTrue("other files should remain");
    }

    [Fact]
    public void DeleteConfigByName_WhenFileDoesNotExist_ShouldNotThrowException()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory(_paths.Config);
        var sut = GetConfigsServices(mockFileSystem);

        // Act
        var act = () => sut.DeleteConfigByName("nonexistent");

        // Assert
        act.Should().NotThrow("deleting non-existent file should not throw");
    }

    [Fact]
    public void DeleteConfigByName_WhenMultipleFilesExist_ShouldDeleteOnlySpecifiedConfig()
    {
        // Arrange
        var configs = new Dictionary<string, string>
        {
            { "music", TestConfigContent.Music },
            { "video", TestConfigContent.Video },
            { "playlist", TestConfigContent.Playlist }
        };
        var mockFileSystem = CreateMockFileSystemWithConfigs(_paths, configs);
        var sut = GetConfigsServices(mockFileSystem);

        // Act
        sut.DeleteConfigByName("video");

        // Assert
        mockFileSystem.File.Exists($"{_paths.Config}music.conf").Should().BeTrue();
        mockFileSystem.File.Exists($"{_paths.Config}video.conf").Should().BeFalse();
        mockFileSystem.File.Exists($"{_paths.Config}playlist.conf").Should().BeTrue();
    }

    #endregion

    #region CreateConfig

    [Fact]
    public async Task CreateNewConfigAsync_WhenFileDoesNotExist_ShouldCreateFileWithSuccessMessage()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory(_paths.Config);
        var sut = GetConfigsServices(mockFileSystem);
        const string configName = "newconfig";
        var configContent = "# New Config\n--format bestvideo";

        // Act
        var result = await sut.CreateNewConfigAsync(configName, configContent);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should()
            .Contain("created successfully")
            .And.Contain(configName);
    }

    [Fact]
    public async Task CreateNewConfigAsync_WhenFileDoesNotExist_ShouldWriteContentCorrectly()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory(_paths.Config);
        var sut = GetConfigsServices(mockFileSystem);
        const string configName = "music";
        var expectedContent = TestConfigContent.Music;

        // Act
        var result = await sut.CreateNewConfigAsync(configName, expectedContent);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var actualContent = mockFileSystem.File.ReadAllText($"{_paths.Config}music.conf");
        actualContent.Should().Be(expectedContent);
    }

    [Fact]
    public async Task CreateNewConfigAsync_WhenFileAlreadyExists_ShouldReturnFailure()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { $"{_paths.Config}existing.conf", new MockFileData("# Existing Config") }
        });
        var sut = GetConfigsServices(mockFileSystem);

        // Act
        var result = await sut.CreateNewConfigAsync("existing", "# New Content");

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("already exists");
    }

    [Fact]
    public async Task CreateNewConfigAsync_WhenFileAlreadyExists_ShouldNotModifyExistingFile()
    {
        // Arrange
        const string originalContent = "# Original Config";
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { $"{_paths.Config}existing.conf", new MockFileData(originalContent) }
        });
        var sut = GetConfigsServices(mockFileSystem);

        // Act
        var result = await sut.CreateNewConfigAsync("existing", "# New Content");

        // Assert
        result.IsFailed.Should().BeTrue();
        var actualContent = mockFileSystem.File.ReadAllText($"{_paths.Config}existing.conf");
        actualContent.Should().Be(originalContent, "existing file should not be modified");
    }

    [Theory]
    [InlineData("music", TestConfigContent.Music)]
    [InlineData("video", TestConfigContent.Video)]
    [InlineData("playlist", TestConfigContent.Playlist)]
    public async Task CreateNewConfigAsync_WithDifferentConfigs_ShouldCreateCorrectly(string configName, string configContent)
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory(_paths.Config);
        var sut = GetConfigsServices(mockFileSystem);

        // Act
        var result = await sut.CreateNewConfigAsync(configName, configContent);

        // Assert
        result.IsSuccess.Should().BeTrue();
        mockFileSystem.File.Exists($"{_paths.Config}{configName}.conf").Should().BeTrue();
        mockFileSystem.File.ReadAllText($"{_paths.Config}{configName}.conf").Should().Be(configContent);
    }

    [Fact]
    public async Task CreateNewConfigAsync_WithEmptyContent_ShouldCreateEmptyFile()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory(_paths.Config);
        var sut = GetConfigsServices(mockFileSystem);

        // Act
        var result = await sut.CreateNewConfigAsync("empty", TestConfigContent.Empty);

        // Assert
        result.IsSuccess.Should().BeTrue();
        mockFileSystem.File.Exists($"{_paths.Config}empty.conf").Should().BeTrue();
        mockFileSystem.File.ReadAllText($"{_paths.Config}empty.conf").Should().BeEmpty();
    }

    [Fact]
    public async Task CreateNewConfigAsync_WithMultilineContent_ShouldPreserveFormatting()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory(_paths.Config);
        var sut = GetConfigsServices(mockFileSystem);
        var multilineContent = TestConfigContent.Multiline;

        // Act
        var result = await sut.CreateNewConfigAsync("multiline", multilineContent);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var actualContent = mockFileSystem.File.ReadAllText($"{_paths.Config}multiline.conf");
        actualContent.Should().Be(multilineContent);
    }

    #endregion

    #region SetConfigContent

    [Fact]
    public async Task SetConfigContentAsync_WhenFileExists_ShouldReturnSuccess()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { $"{_paths.Config}music.conf", new MockFileData("# Old Content") }
        });
        var sut = GetConfigsServices(mockFileSystem);
        var newContent = "# Updated Content\n--format bestvideo";

        // Act
        var result = await sut.SetConfigContentAsync("music", newContent);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SetConfigContentAsync_WhenFileExists_ShouldUpdateContentCompletely()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { $"{_paths.Config}music.conf", new MockFileData("# Old Content") }
        });
        var sut = GetConfigsServices(mockFileSystem);
        var newContent = TestConfigContent.Music;

        // Act
        await sut.SetConfigContentAsync("music", newContent);

        // Assert
        var actualContent = mockFileSystem.File.ReadAllText($"{_paths.Config}music.conf");
        actualContent.Should().Be(newContent);
    }

    [Fact]
    public async Task SetConfigContentAsync_WhenFileDoesNotExist_ShouldReturnFailure()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory(_paths.Config);
        var sut = GetConfigsServices(mockFileSystem);

        // Act
        var result = await sut.SetConfigContentAsync("nonexistent", "# New Content");

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should()
            .Contain("doesnt exists")
            .And.Contain("nonexistent");
    }

    [Fact]
    public async Task SetConfigContentAsync_WhenFileDoesNotExist_ShouldNotCreateFile()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory(_paths.Config);
        var sut = GetConfigsServices(mockFileSystem);

        // Act
        await sut.SetConfigContentAsync("nonexistent", "# New Content");

        // Assert
        mockFileSystem.File.Exists($"{_paths.Config}nonexistent.conf").Should().BeFalse();
    }

    [Theory]
    [InlineData("music", TestConfigContent.Music)]
    [InlineData("video", TestConfigContent.Video)]
    [InlineData("playlist", TestConfigContent.Playlist)]
    public async Task SetConfigContentAsync_WithDifferentConfigs_ShouldUpdateCorrectly(string configName, string newContent)
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { $"{_paths.Config}{configName}.conf", new MockFileData("# Old Content") }
        });
        var sut = GetConfigsServices(mockFileSystem);

        // Act
        var result = await sut.SetConfigContentAsync(configName, newContent);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var actualContent = mockFileSystem.File.ReadAllText($"{_paths.Config}{configName}.conf");
        actualContent.Should().Be(newContent);
    }

    [Fact]
    public async Task SetConfigContentAsync_WithEmptyContent_ShouldOverwriteWithEmpty()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { $"{_paths.Config}test.conf", new MockFileData("# Original Content") }
        });
        var sut = GetConfigsServices(mockFileSystem);

        // Act
        var result = await sut.SetConfigContentAsync("test", TestConfigContent.Empty);

        // Assert
        result.IsSuccess.Should().BeTrue();
        mockFileSystem.File.ReadAllText($"{_paths.Config}test.conf").Should().BeEmpty();
    }

    [Fact]
    public async Task SetConfigContentAsync_ShouldCompletelyReplaceOldContent()
    {
        // Arrange
        const string originalContent = "# Very Long Original Content\n--option1\n--option2\n--option3";
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { $"{_paths.Config}test.conf", new MockFileData(originalContent) }
        });
        var sut = GetConfigsServices(mockFileSystem);
        const string newContent = "# Short";

        // Act
        await sut.SetConfigContentAsync("test", newContent);

        // Assert
        var actualContent = mockFileSystem.File.ReadAllText($"{_paths.Config}test.conf");
        actualContent.Should().Be(newContent)
            .And.NotContain("option1");
    }

    #endregion

    #region SplitArguments

    [Theory]
    [InlineData("-o \"file.mp4\" -f best", new[] { "-o \"file.mp4\"", "-f best" })]
    [InlineData("--output '%(title)s.%(ext)s'", new[] { "--output '%(title)s.%(ext)s'" })]
    [InlineData("-f best -o output.mp4", new[] { "-f best", "-o output.mp4" })]
    [InlineData("--format bestvideo", new[] { "--format bestvideo" })]
    public void SplitArguments_WithVariousFormats_ShouldSplitCorrectly(string input, string[] expected)
    {
        // Arrange
        var sut = GetConfigsServices();

        // Act
        var result = sut.SplitArguments(input);

        // Assert
        result.Should().Equal(expected);
    }

    [Fact]
    public void SplitArguments_WithEmptyString_ShouldReturnEmptyList()
    {
        // Arrange
        var sut = GetConfigsServices();

        // Act
        var result = sut.SplitArguments(string.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region FixConfigContent

    [Fact]
    public void FixConfigContent_WithEmptyString_ShouldReturnEmptyString()
    {
        // Arrange
        var sut = GetConfigsServices();

        // Act
        var result = sut.FixConfigContent(TestConfigContent.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void FixConfigContent_WithWhitespaceOnly_ShouldHandleGracefully(string content)
    {
        // Arrange
        var sut = GetConfigsServices();

        // Act
        var result = sut.FixConfigContent(content);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void FixConfigContent_WithCommentsOnly_ShouldPreserveAllComments()
    {
        // Arrange
        var sut = GetConfigsServices();
        var content = TestConfigContent.CommentOnly;

        // Act
        var result = sut.FixConfigContent(content);

        // Assert
        result.Should()
            .Contain("# This is a comment")
            .And.Contain("# Another comment")
            .And.Contain("#Third comment");
    }

    [Fact]
    public void FixConfigContent_WithMixedCommentsAndContent_ShouldPreserveComments()
    {
        // Arrange
        var mockPathParser = new Mock<PathParserSerivce>();
        mockPathParser.Setup(p => p.CheckAndFixPaths(It.IsAny<string>()))
            .Returns<string>(s => s);

        var sut = GetConfigsServices(pathParserSerivce: mockPathParser.Object);
        var content = "# Comment\nsome config line\n# Another comment";

        // Act
        var result = sut.FixConfigContent(content);

        // Assert
        result.Should()
            .Contain("# Comment")
            .And.Contain("# Another comment");
    }

    [Fact]
    public void FixConfigContent_WithSingleConfigLine_ShouldCallPathParser()
    {
        // Arrange
        var mockPathParser = new Mock<PathParserSerivce>();
        mockPathParser.Setup(p => p.CheckAndFixPaths("config_value"))
            .Returns("/fixed/path/config_value");

        var sut = GetConfigsServices(pathParserSerivce: mockPathParser.Object);

        // Act
        var result = sut.FixConfigContent("config_value");

        // Assert
        mockPathParser.Verify(
            p => p.CheckAndFixPaths("config_value"),
            Times.Once,
            "Path parser should be called exactly once");
        result.Should().Contain("/fixed/path/config_value");
    }

    [Fact]
    public void FixConfigContent_WithMultilineInput_ShouldReturnFormattedString()
    {
        // Arrange
        var sut = GetConfigsServices();
        var input = "--format bestvideo\n-o output.mp4";

        // Act
        var result = sut.FixConfigContent(input);

        // Assert
        result.Should()
            .Contain(Environment.NewLine)
            .And.Contain("--format")
            .And.Contain(_paths.Downloads);
    }

    [Fact]
    public void FixConfigContent_WithCommentsAndArgs_ShouldPreserveBoth()
    {
        // Arrange
        var sut = GetConfigsServices();
        var input = TestConfigContent.Complex;

        // Act
        var result = sut.FixConfigContent(input);

        // Assert
        result.Should()
            .Contain("# Download config")
            .And.Contain("# Output settings")
            .And.Contain("--format");
    }

    [Fact]
    public void FixConfigContent_WithComplexConfigFile_ShouldFixAllPaths()
    {
        // Arrange
        var sut = GetConfigsServices();
        var input = @"--format bestvideo
-o video.mp4
--paths home:/downloads
# End of config";

        // Act
        var result = sut.FixConfigContent(input);

        // Assert
        var lines = result.Split(Environment.NewLine);
        lines.Should().Contain(line =>
            line.Contains(_paths.Downloads) && line.Contains("video.mp4"))
            .And.Contain(line =>
                line.Contains(_paths.Downloads) && line.Contains("home:"))
            .And.Contain("# End of config");
    }

    [Fact]
    public void FixConfigContent_WithPathsContainingSpaces_ShouldHandleCorrectly()
    {
        // Arrange
        var mockPathParser = new Mock<PathParserSerivce>();
        mockPathParser.Setup(p => p.CheckAndFixPaths("/path/with spaces"))
            .Returns("/path/with spaces");

        var sut = GetConfigsServices(pathParserSerivce: mockPathParser.Object);

        // Act
        var result = sut.FixConfigContent("/path/with spaces");

        // Assert
        result.Should().NotBeEmpty();
    }

    [Fact]
    public void FixConfigContent_WithLeadingAndTrailingWhitespace_TrimsCorrectly()
    {
        // Arrange
        var mockPathParser = new Mock<PathParserSerivce>();
        mockPathParser.Setup(p => p.CheckAndFixPaths("config"))
            .Returns("fixed_config");

        var sut = GetConfigsServices(pathParserSerivce: mockPathParser.Object);

        // Act
        var result = sut.FixConfigContent("   config   ");

        // Assert
        mockPathParser.Verify(
            p => p.CheckAndFixPaths("config"),
            Times.Once,
            "Should process trimmed content");
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
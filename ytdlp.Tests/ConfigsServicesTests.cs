using System.IO.Abstractions.TestingHelpers;
using Microsoft.Extensions.Options;
using ytdlp.Configs;
using FluentAssertions;
using ytdlp.Services;
using FluentResults;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualBasic;

namespace ytdlp.Tests.Services;

[ExcludeFromCodeCoverage]
public class ConfigsServicesTests
{
    #region Helpers
    private ConfigsServices GetConfigsServices(
        MockFileSystem? mockFileSystem = null,
        IOptions<PathConfiguration>? iOptionsPathConfig = null,
        PathConfiguration? pathConfiguration = null,
        PathParserSerivce? pathParserSerivce = null
        )
    {
        mockFileSystem ??= new MockFileSystem();

        iOptionsPathConfig ??= pathConfiguration != null
            ? Options.Create(pathConfiguration)
            : Options.Create(paths);

        pathParserSerivce ??= new PathParserSerivce(iOptionsPathConfig);

        return new ConfigsServices(mockFileSystem, iOptionsPathConfig, pathParserSerivce);
    }
    private readonly PathConfiguration paths = new();

    #endregion
    #region GetConfigPath
    [Fact]
    public void GetWholeConfigPath_ShouldReturnCorrectPath()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var sut = GetConfigsServices(mockFileSystem);
        var configName = "testconfig";

        // Act
        var result = sut.GetWholeConfigPath(configName);

        // Assert
        result.Should().Be($"{paths.Config}{configName}.conf");
    }

    [Theory]
    [InlineData("music")]
    [InlineData("video")]
    [InlineData("playlist")]
    public void GetWholeConfigPath_WithDifferentNames_ShouldReturnCorrectPath(string configName)
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var sut = GetConfigsServices(mockFileSystem);

        // Act
        var result = sut.GetWholeConfigPath(configName);

        // Assert
        result.Should().Be($"{paths.Config}{configName}.conf");
    }

    #endregion
    #region GetAllConfigNames
    [Fact]
    public void GetAllConfigNames_WithMultipleConfigs_ShouldReturnAllNames()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
    {
        { $"{paths.Config}music.conf", new MockFileData("") },
        { $"{paths.Config}video.conf", new MockFileData("") },
        { $"{paths.Config}playlist.conf", new MockFileData("") }
    });
        var sut = GetConfigsServices(mockFileSystem);

        // Act
        var result = sut.GetAllConfigNames();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(["music", "video", "playlist"]);
    }

    [Fact]
    public void GetAllConfigNames_WithNoConfigs_ShouldReturnEmptyList()
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
    public void GetAllConfigNames_WithMixedFiles_ShouldReturnOnlyConfFiles()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
    {
        { $"{paths.Config}music.conf", new MockFileData("") },
        { $"{paths.Config}readme.txt", new MockFileData("") },
        { $"{paths.Config}video.conf", new MockFileData("") },
        { $"{paths.Config}backup.bak", new MockFileData("") }
    });
        var sut = GetConfigsServices(mockFileSystem);

        // Act
        var result = sut.GetAllConfigNames();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(new[] { "music", "video" });
        result.Should().NotContain("readme");
        result.Should().NotContain("backup");
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
        result.Should().Contain("test");
        result.Should().NotContain("test.conf");
    }

    #endregion

    #region GetConfigContent
    [Fact]
    public void GetConfigContentByName_WhenFileExists_ShouldReturnContentSuccessfully()
    {
        // Arrange
        var expectedContent = "--format bestvideo+bestaudio\n--output '%(title)s.%(ext)s'";
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
    {
        { $"{paths.Config}music.conf", new MockFileData(expectedContent) }
    });
        var sut = GetConfigsServices(mockFileSystem);

        // Act
        var result = sut.GetConfigContentByName("music");

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
        result.Errors[0].Message.Should().Contain("Config file not found");
        result.Errors[0].Message.Should().Contain($"{paths.Config}nonexistent.conf");
    }

    [Theory]
    [InlineData("music", "--extract-audio\n--audio-format mp3")]
    [InlineData("video", "--format bestvideo")]
    [InlineData("playlist", "--yes-playlist")]
    public void GetConfigContentByName_WithDifferentConfigs_ShouldReturnCorrectContent(string configName, string expectedContent)
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
    public void GetConfigContentByName_WhenFileIsEmpty_ShouldReturnEmptyString()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
    {
        { $"{paths.Config}empty.conf", new MockFileData("") }
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
    public async Task DeleteConfigByName_WhenFileExists_ShouldDeleteFile()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
    {
        { $"{paths.Config}music.conf", new MockFileData("# Music Config") }
    });
        var sut = GetConfigsServices(mockFileSystem);

        // Act
        sut.DeleteConfigByName("music");

        // Assert
        mockFileSystem.File.Exists($"{paths.Config}music.conf").Should().BeFalse();
    }

    [Theory]
    [InlineData("music")]
    [InlineData("video")]
    [InlineData("playlist")]
    public async Task DeleteConfigByName_WithDifferentNames_ShouldDeleteCorrectFile(string configName)
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
    {
        { $"{paths.Config}{configName}.conf", new MockFileData("# Config Content") },
        { $"{paths.Config}other.conf", new MockFileData("# Other Config") }
    });
        var sut = GetConfigsServices(mockFileSystem);

        // Act
        sut.DeleteConfigByName(configName);

        // Assert
        mockFileSystem.File.Exists($"{paths.Config}{configName}.conf").Should().BeFalse();
        mockFileSystem.File.Exists($"{paths.Config}other.conf").Should().BeTrue();
    }

    [Fact]
    public async Task DeleteConfigByName_WhenFileDoesNotExist_ShouldNotThrowException()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory(paths.Config);
        var sut = GetConfigsServices(mockFileSystem);

        // Act
        Func<Task> act = async () => sut.DeleteConfigByName("nonexistent");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteConfigByName_WhenMultipleFilesExist_ShouldOnlyDeleteSpecifiedFile()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
    {
        { $"{paths.Config}music.conf", new MockFileData("# Music") },
        { $"{paths.Config}video.conf", new MockFileData("# Video") },
        { $"{paths.Config}playlist.conf", new MockFileData("# Playlist") }
    });
        var sut = GetConfigsServices(mockFileSystem);

        // Act
        sut.DeleteConfigByName("video");

        // Assert
        mockFileSystem.File.Exists($"{paths.Config}music.conf").Should().BeTrue();
        mockFileSystem.File.Exists($"{paths.Config}video.conf").Should().BeFalse();
        mockFileSystem.File.Exists($"{paths.Config}playlist.conf").Should().BeTrue();
    }

    #endregion

    #region CreateConfig
    [Fact]
    public async Task CreateNewConfig_WhenFileDoesNotExist_ShouldCreateFileSuccessfully()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory($"{paths.Config}");
        var sut = GetConfigsServices(mockFileSystem);
        var configName = "newconfig";
        var configContent = "# New Config\n--format bestvideo";

        // Act
        var result = await sut.CreateNewConfigAsync(configName, configContent);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("created successfully");
        result.Value.Should().Contain(configName);
        mockFileSystem.File.Exists($"{paths.Config}newconfig.conf").Should().BeTrue();
    }

    [Fact]
    public async Task CreateNewConfig_WhenFileDoesNotExist_ShouldWriteContentCorrectly()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory($"{paths.Config}");
        var sut = GetConfigsServices(mockFileSystem);
        var configName = "music";
        var expectedContent = "# Music Config\n--extract-audio\n--audio-format mp3";

        // Act
        var result = await sut.CreateNewConfigAsync(configName, expectedContent);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var actualContent = mockFileSystem.File.ReadAllText($"{paths.Config}music.conf");
        actualContent.Should().Be(expectedContent);
    }

    [Fact]
    public async Task CreateNewConfig_WhenFileAlreadyExists_ShouldReturnFailureAsync()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
    {
        { $"{paths.Config}existing.conf", new MockFileData("# Existing Config") }
    });
        var sut = GetConfigsServices(mockFileSystem);

        // Act
        var result = await sut.CreateNewConfigAsync("existing", "# New Content");

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Message.Should().Contain("already exists");
    }

    [Fact]
    public async Task CreateNewConfig_WhenFileAlreadyExists_ShouldNotModifyExistingFileAsync()
    {
        // Arrange
        var originalContent = "# Original Config";
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
    {
        { $"{paths.Config}existing.conf", new MockFileData(originalContent) }
    });
        var sut = GetConfigsServices(mockFileSystem);

        // Act
        var result = await sut.CreateNewConfigAsync("existing", "# New Content");

        // Assert
        result.IsFailed.Should().BeTrue();
        var actualContent = mockFileSystem.File.ReadAllText($"{paths.Config}existing.conf");
        actualContent.Should().Be(originalContent);
    }

    [Theory]
    [InlineData("music", "# Music\n--extract-audio")]
    [InlineData("video", "# Video\n--format bestvideo")]
    [InlineData("playlist", "# Playlist\n--yes-playlist")]
    public async Task CreateNewConfig_WithDifferentNamesAndContent_ShouldCreateCorrectlyAsync(string configName, string configContent)
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory($"{paths.Config}");
        var sut = GetConfigsServices(mockFileSystem);

        // Act
        var result = await sut.CreateNewConfigAsync(configName, configContent);

        // Assert
        result.IsSuccess.Should().BeTrue();
        mockFileSystem.File.Exists($"{paths.Config}{configName}.conf").Should().BeTrue();
        mockFileSystem.File.ReadAllText($"{paths.Config}{configName}.conf").Should().Be(configContent);
    }

    [Fact]
    public async Task CreateNewConfig_WithEmptyContent_ShouldCreateEmptyFileAsync()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory($"{paths.Config}");
        var sut = GetConfigsServices(mockFileSystem);

        // Act
        var result = await sut.CreateNewConfigAsync("empty", "");

        // Assert
        result.IsSuccess.Should().BeTrue();
        mockFileSystem.File.Exists($"{paths.Config}empty.conf").Should().BeTrue();
        mockFileSystem.File.ReadAllText($"{paths.Config}empty.conf").Should().BeEmpty();
    }

    [Fact]
    public async Task CreateNewConfig_WithMultilineContent_ShouldPreserveFormattingAsync()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory($"{paths.Config}");
        var sut = GetConfigsServices(mockFileSystem);
        var multilineContent = "# Config Header\n--format bestvideo\n--output '%(title)s'\n# Comment";

        // Act
        var result = await sut.CreateNewConfigAsync("multiline", multilineContent);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var actualContent = mockFileSystem.File.ReadAllText($"{paths.Config}multiline.conf");
        actualContent.Should().Be(multilineContent);
    }
    #endregion

    #region PatchConfig
    [Fact]
    public async Task SetConfigContentAsync_WhenFileExists_ShouldReturnSuccess()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
    {
        { $"{paths.Config}music.conf", new MockFileData("# Old Content") }
    });
        var sut = GetConfigsServices(mockFileSystem);
        var newContent = "# Updated Content\n--format bestvideo";

        // Act
        var result = await sut.SetConfigContentAsync("music", newContent);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SetConfigContentAsync_WhenFileExists_ShouldUpdateContent()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
    {
        { $"{paths.Config}music.conf", new MockFileData("# Old Content") }
    });
        var sut = GetConfigsServices(mockFileSystem);
        var newContent = "# Updated Content\n--extract-audio\n--audio-format mp3";

        // Act
        await sut.SetConfigContentAsync("music", newContent);

        // Assert
        var actualContent = mockFileSystem.File.ReadAllText($"{paths.Config}music.conf");
        actualContent.Should().Be(newContent);
    }

    [Fact]
    public async Task SetConfigContentAsync_WhenFileDoesNotExist_ShouldReturnFailure()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory($"{paths.Config}");
        var sut = GetConfigsServices(mockFileSystem);

        // Act
        var result = await sut.SetConfigContentAsync("nonexistent", "# New Content");

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Message.Should().Contain("doesnt exists");
        result.Errors[0].Message.Should().Contain("nonexistent");
    }

    [Fact]
    public async Task SetConfigContentAsync_WhenFileDoesNotExist_ShouldNotCreateFile()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory($"{paths.Config}");
        var sut = GetConfigsServices(mockFileSystem);

        // Act
        await sut.SetConfigContentAsync("nonexistent", "# New Content");

        // Assert
        mockFileSystem.File.Exists($"{paths.Config}nonexistent.conf").Should().BeFalse();
    }

    [Theory]
    [InlineData("music", "# Music Config\n--extract-audio")]
    [InlineData("video", "# Video Config\n--format bestvideo")]
    [InlineData("playlist", "# Playlist Config\n--yes-playlist")]
    public async Task SetConfigContentAsync_WithDifferentConfigs_ShouldUpdateCorrectly(string configName, string newContent)
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
    {
        { $"{paths.Config}{configName}.conf", new MockFileData("# Old Content") }
    });
        var sut = GetConfigsServices(mockFileSystem);

        // Act
        var result = await sut.SetConfigContentAsync(configName, newContent);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var actualContent = mockFileSystem.File.ReadAllText($"{paths.Config}{configName}.conf");
        actualContent.Should().Be(newContent);
    }

    [Fact]
    public async Task SetConfigContentAsync_WithEmptyContent_ShouldOverwriteWithEmpty()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
    {
        { $"{paths.Config}test.conf", new MockFileData("# Original Content") }
    });
        var sut = GetConfigsServices(mockFileSystem);

        // Act
        var result = await sut.SetConfigContentAsync("test", "");

        // Assert
        result.IsSuccess.Should().BeTrue();
        mockFileSystem.File.ReadAllText($"{paths.Config}test.conf").Should().BeEmpty();
    }

    [Fact]
    public async Task SetConfigContentAsync_ShouldCompletelyReplaceOldContent()
    {
        // Arrange
        var originalContent = "# Very Long Original Content\n--option1\n--option2\n--option3";
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
    {
        { $"{paths.Config}test.conf", new MockFileData(originalContent) }
    });
        var sut = GetConfigsServices(mockFileSystem);
        var newContent = "# Short";

        // Act
        await sut.SetConfigContentAsync("test", newContent);

        // Assert
        var actualContent = mockFileSystem.File.ReadAllText($"{paths.Config}test.conf");
        actualContent.Should().Be(newContent);
        actualContent.Should().NotContain("option1");
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
    var result = sut.SplitArguments("");

    // Assert
    result.Should().BeEmpty();
}
#endregion

#region FixConfigContent
[Fact]
public void FixConfigContent_WithMultilineInput_ShouldReturnFormattedString()
{
    // Arrange
    var sut = GetConfigsServices();
    var input = "--format bestvideo\n-o output.mp4";

    // Act
    var result = sut.FixConfigContent(input);

    // Assert
    result.Should().Contain(Environment.NewLine);
    result.Should().Contain("--format");
    result.Should().Contain(paths.Downloads);
}

[Fact]
public void FixConfigContent_WithCommentsAndArgs_ShouldPreserveBoth()
{
    // Arrange
    var sut = GetConfigsServices();
    var input = "# Download config\n--format bestvideo\n# Output settings\n-o video.mp4";

    // Act
    var result = sut.FixConfigContent(input);

    // Assert
    result.Should().Contain("# Download config");
    result.Should().Contain("# Output settings");
    result.Should().Contain("--format");
}

[Fact]
public void FixConfigContent_WithEmptyString_ShouldReturnEmptyString()
{
    // Arrange
    var sut = GetConfigsServices();
    var input = "";

    // Act
    var result = sut.FixConfigContent(input);

    // Assert
    result.Should().BeEmpty();
}

[Fact]
public void FixConfigContent_WithComplexConfig_ShouldFixAllPaths()
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
    lines.Should().Contain(line => line.Contains(paths.Downloads) && line.Contains("video.mp4"));
    lines.Should().Contain(line => line.Contains(paths.Downloads) && line.Contains("home:"));
    lines.Should().Contain("# End of config");
}
#endregion

}

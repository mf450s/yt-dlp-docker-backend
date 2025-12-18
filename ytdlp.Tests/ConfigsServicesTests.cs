using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using ytdlp.Services;
using FluentResults;
using System.Diagnostics.CodeAnalysis;

namespace ytdlp.Tests.Services;
[ExcludeFromCodeCoverage]
public class ConfigsServicesTests
{
    #region GetConfigPath
    [Fact]
    public void GetWholeConfigPath_ShouldReturnCorrectPath()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var sut = new ConfigsServices(mockFileSystem);
        var configName = "testconfig";

        // Act
        var result = sut.GetWholeConfigPath(configName);

        // Assert
        result.Should().Be("../configs/testconfig.conf");
    }

    [Theory]
    [InlineData("music")]
    [InlineData("video")]
    [InlineData("playlist")]
    public void GetWholeConfigPath_WithDifferentNames_ShouldReturnCorrectPath(string configName)
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var sut = new ConfigsServices(mockFileSystem);

        // Act
        var result = sut.GetWholeConfigPath(configName);

        // Assert
        result.Should().Be($"../configs/{configName}.conf");
    }
    #endregion

    #region GetAllConfigNames
    [Fact]
    public void GetAllConfigNames_WithMultipleConfigs_ShouldReturnAllNames()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { "../configs/music.conf", new MockFileData("") },
            { "../configs/video.conf", new MockFileData("") },
            { "../configs/playlist.conf", new MockFileData("") }
        });
        var sut = new ConfigsServices(mockFileSystem);

        // Act
        var result = sut.GetAllConfigNames();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(new[] { "music", "video", "playlist" });
    }

    [Fact]
    public void GetAllConfigNames_WithNoConfigs_ShouldReturnEmptyList()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory("../configs/");
        var sut = new ConfigsServices(mockFileSystem);

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
            { "../configs/music.conf", new MockFileData("") },
            { "../configs/readme.txt", new MockFileData("") },
            { "../configs/video.conf", new MockFileData("") },
            { "../configs/backup.bak", new MockFileData("") }
        });
        var sut = new ConfigsServices(mockFileSystem);

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
            { "../configs/test.conf", new MockFileData("") }
        });
        var sut = new ConfigsServices(mockFileSystem);

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
        { "../configs/music.conf", new MockFileData(expectedContent) }
    });
        var sut = new ConfigsServices(mockFileSystem);

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
        mockFileSystem.AddDirectory("../configs/");
        var sut = new ConfigsServices(mockFileSystem);

        // Act
        var result = sut.GetConfigContentByName("nonexistent");

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Message.Should().Contain("Config file not found");
        result.Errors[0].Message.Should().Contain("../configs/nonexistent.conf");
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
        { $"../configs/{configName}.conf", new MockFileData(expectedContent) }
    });
        var sut = new ConfigsServices(mockFileSystem);

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
        { "../configs/empty.conf", new MockFileData("") }
    });
        var sut = new ConfigsServices(mockFileSystem);

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
        { "../configs/music.conf", new MockFileData("# Music Config") }
    });
        var sut = new ConfigsServices(mockFileSystem);

        // Act
        sut.DeleteConfigByName("music");

        // Assert
        mockFileSystem.File.Exists("../configs/music.conf").Should().BeFalse();
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
        { $"../configs/{configName}.conf", new MockFileData("# Config Content") },
        { "../configs/other.conf", new MockFileData("# Other Config") }
    });
        var sut = new ConfigsServices(mockFileSystem);

        // Act
        sut.DeleteConfigByName(configName);

        // Assert
        mockFileSystem.File.Exists($"../configs/{configName}.conf").Should().BeFalse();
        mockFileSystem.File.Exists("../configs/other.conf").Should().BeTrue();
    }

    [Fact]
    public async Task DeleteConfigByName_WhenFileDoesNotExist_ShouldNotThrowException()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory("../configs/");
        var sut = new ConfigsServices(mockFileSystem);

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
        { "../configs/music.conf", new MockFileData("# Music") },
        { "../configs/video.conf", new MockFileData("# Video") },
        { "../configs/playlist.conf", new MockFileData("# Playlist") }
    });
        var sut = new ConfigsServices(mockFileSystem);

        // Act
        sut.DeleteConfigByName("video");

        // Assert
        mockFileSystem.File.Exists("../configs/music.conf").Should().BeTrue();
        mockFileSystem.File.Exists("../configs/video.conf").Should().BeFalse();
        mockFileSystem.File.Exists("../configs/playlist.conf").Should().BeTrue();
    }
    #endregion

    #region CreateConfig
    [Fact]
    public async Task CreateNewConfig_WhenFileDoesNotExist_ShouldCreateFileSuccessfully()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory("../configs/");
        var sut = new ConfigsServices(mockFileSystem);
        var configName = "newconfig";
        var configContent = "# New Config\n--format bestvideo";

        // Act
        var result = await sut.CreateNewConfigAsync(configName, configContent);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("created successfully");
        result.Value.Should().Contain(configName);
        mockFileSystem.File.Exists("../configs/newconfig.conf").Should().BeTrue();
    }

    [Fact]
    public async Task CreateNewConfig_WhenFileDoesNotExist_ShouldWriteContentCorrectly()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory("../configs/");
        var sut = new ConfigsServices(mockFileSystem);
        var configName = "music";
        var expectedContent = "# Music Config\n--extract-audio\n--audio-format mp3";

        // Act
        var result = await sut.CreateNewConfigAsync(configName, expectedContent);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var actualContent = mockFileSystem.File.ReadAllText("../configs/music.conf");
        actualContent.Should().Be(expectedContent);
    }

    [Fact]
    public async Task CreateNewConfig_WhenFileAlreadyExists_ShouldReturnFailureAsync()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
    {
        { "../configs/existing.conf", new MockFileData("# Existing Config") }
    });
        var sut = new ConfigsServices(mockFileSystem);

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
        { "../configs/existing.conf", new MockFileData(originalContent) }
    });
        var sut = new ConfigsServices(mockFileSystem);

        // Act
        var result = await sut.CreateNewConfigAsync("existing", "# New Content");

        // Assert
        result.IsFailed.Should().BeTrue();
        var actualContent = mockFileSystem.File.ReadAllText("../configs/existing.conf");
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
        mockFileSystem.AddDirectory("../configs/");
        var sut = new ConfigsServices(mockFileSystem);

        // Act
        var result = await sut.CreateNewConfigAsync(configName, configContent);

        // Assert
        result.IsSuccess.Should().BeTrue();
        mockFileSystem.File.Exists($"../configs/{configName}.conf").Should().BeTrue();
        mockFileSystem.File.ReadAllText($"../configs/{configName}.conf").Should().Be(configContent);
    }

    [Fact]
    public async Task CreateNewConfig_WithEmptyContent_ShouldCreateEmptyFileAsync()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory("../configs/");
        var sut = new ConfigsServices(mockFileSystem);

        // Act
        var result = await sut.CreateNewConfigAsync("empty", "");

        // Assert
        result.IsSuccess.Should().BeTrue();
        mockFileSystem.File.Exists("../configs/empty.conf").Should().BeTrue();
        mockFileSystem.File.ReadAllText("../configs/empty.conf").Should().BeEmpty();
    }

    [Fact]
    public async Task CreateNewConfig_WithMultilineContent_ShouldPreserveFormattingAsync()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory("../configs/");
        var sut = new ConfigsServices(mockFileSystem);
        var multilineContent = "# Config Header\n--format bestvideo\n--output '%(title)s'\n# Comment";

        // Act
        var result = await sut.CreateNewConfigAsync("multiline", multilineContent);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var actualContent = mockFileSystem.File.ReadAllText("../configs/multiline.conf");
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
        { "../configs/music.conf", new MockFileData("# Old Content") }
    });
        var sut = new ConfigsServices(mockFileSystem);
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
        { "../configs/music.conf", new MockFileData("# Old Content") }
    });
        var sut = new ConfigsServices(mockFileSystem);
        var newContent = "# Updated Content\n--extract-audio\n--audio-format mp3";

        // Act
        await sut.SetConfigContentAsync("music", newContent);

        // Assert
        var actualContent = mockFileSystem.File.ReadAllText("../configs/music.conf");
        actualContent.Should().Be(newContent);
    }

    [Fact]
    public async Task SetConfigContentAsync_WhenFileDoesNotExist_ShouldReturnFailure()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory("../configs/");
        var sut = new ConfigsServices(mockFileSystem);

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
        mockFileSystem.AddDirectory("../configs/");
        var sut = new ConfigsServices(mockFileSystem);

        // Act
        await sut.SetConfigContentAsync("nonexistent", "# New Content");

        // Assert
        mockFileSystem.File.Exists("../configs/nonexistent.conf").Should().BeFalse();
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
        { $"../configs/{configName}.conf", new MockFileData("# Old Content") }
    });
        var sut = new ConfigsServices(mockFileSystem);

        // Act
        var result = await sut.SetConfigContentAsync(configName, newContent);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var actualContent = mockFileSystem.File.ReadAllText($"../configs/{configName}.conf");
        actualContent.Should().Be(newContent);
    }

    [Fact]
    public async Task SetConfigContentAsync_WithEmptyContent_ShouldOverwriteWithEmpty()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
    {
        { "../configs/test.conf", new MockFileData("# Original Content") }
    });
        var sut = new ConfigsServices(mockFileSystem);

        // Act
        var result = await sut.SetConfigContentAsync("test", "");

        // Assert
        result.IsSuccess.Should().BeTrue();
        mockFileSystem.File.ReadAllText("../configs/test.conf").Should().BeEmpty();
    }

    [Fact]
    public async Task SetConfigContentAsync_ShouldCompletelyReplaceOldContent()
    {
        // Arrange
        var originalContent = "# Very Long Original Content\n--option1\n--option2\n--option3";
        var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
    {
        { "../configs/test.conf", new MockFileData(originalContent) }
    });
        var sut = new ConfigsServices(mockFileSystem);
        var newContent = "# Short";

        // Act
        await sut.SetConfigContentAsync("test", newContent);

        // Assert
        var actualContent = mockFileSystem.File.ReadAllText("../configs/test.conf");
        actualContent.Should().Be(newContent);
        actualContent.Should().NotContain("option1");
    }
    #endregion
}

using Lfm.Core.Configuration;
using Lfm.Core.Services;
using Lfm.Shared.Services;
using Lfm.Shared.Configuration;
using FluentAssertions;
using Moq;

namespace Lfm.Tests.Unit.Services;

/// <summary>
/// Unit tests for SymbolProvider - platform-specific Unicode/ASCII symbol selection
/// </summary>
[Trait("Category", "Unit")]
public class SymbolProviderTests
{
    [Fact]
    public void SymbolProvider_WithUnicodeEnabled_ReturnsUnicodeSymbols()
    {
        // Arrange
        var mockConfigManager = new Mock<IConfigurationManager>();
        var config = new LfmConfig { UnicodeSymbols = UnicodeSupport.Enabled };
        mockConfigManager.Setup(m => m.LoadAsync()).ReturnsAsync(config);

        // Act
        var provider = new SymbolProvider(mockConfigManager.Object);

        // Assert
        provider.Error.Should().Be("❌");
        provider.Success.Should().Be("✅");
        provider.Tip.Should().Be("💡");
        provider.Timer.Should().Be("⏱️");
        provider.Music.Should().Be("🎵");
    }

    [Fact]
    public void SymbolProvider_WithUnicodeDisabled_ReturnsAsciiSymbols()
    {
        // Arrange
        var mockConfigManager = new Mock<IConfigurationManager>();
        var config = new LfmConfig { UnicodeSymbols = UnicodeSupport.Disabled };
        mockConfigManager.Setup(m => m.LoadAsync()).ReturnsAsync(config);

        // Act
        var provider = new SymbolProvider(mockConfigManager.Object);

        // Assert
        provider.Error.Should().Be("[X]");
        provider.Success.Should().Be("[OK]");
        provider.Tip.Should().Be("[TIP]");
        provider.Timer.Should().Be("[TIME]");
        provider.Music.Should().Be("[MUSIC]");
    }

    [Fact]
    public void SymbolProvider_AllSymbolsAreDefined()
    {
        // Arrange
        var mockConfigManager = new Mock<IConfigurationManager>();
        var config = new LfmConfig { UnicodeSymbols = UnicodeSupport.Disabled };
        mockConfigManager.Setup(m => m.LoadAsync()).ReturnsAsync(config);

        // Act
        var provider = new SymbolProvider(mockConfigManager.Object);

        // Assert - All symbols should be non-null and non-empty
        provider.Error.Should().NotBeNullOrWhiteSpace();
        provider.Success.Should().NotBeNullOrWhiteSpace();
        provider.Tip.Should().NotBeNullOrWhiteSpace();
        provider.Timer.Should().NotBeNullOrWhiteSpace();
        provider.Stats.Should().NotBeNullOrWhiteSpace();
        provider.Settings.Should().NotBeNullOrWhiteSpace();
        provider.Cleanup.Should().NotBeNullOrWhiteSpace();
        provider.Music.Should().NotBeNullOrWhiteSpace();
        provider.Clipboard.Should().NotBeNullOrWhiteSpace();
        provider.StopSign.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void SymbolProvider_WithConfigLoadFailure_UsesAutoDetection()
    {
        // Arrange
        var mockConfigManager = new Mock<IConfigurationManager>();
        mockConfigManager.Setup(m => m.LoadAsync()).ThrowsAsync(new Exception("Config load failed"));

        // Act - Should not throw, should fall back to auto-detection
        Action act = () => new SymbolProvider(mockConfigManager.Object);

        // Assert
        act.Should().NotThrow("config load failure should be handled gracefully");
    }

    [Fact]
    public void SymbolProvider_UnicodeAndAsciiSymbolsAreDifferent()
    {
        // Arrange
        var mockConfigManager = new Mock<IConfigurationManager>();

        var unicodeConfig = new LfmConfig { UnicodeSymbols = UnicodeSupport.Enabled };
        var asciiConfig = new LfmConfig { UnicodeSymbols = UnicodeSupport.Disabled };

        // Act
        mockConfigManager.Setup(m => m.LoadAsync()).ReturnsAsync(unicodeConfig);
        var unicodeProvider = new SymbolProvider(mockConfigManager.Object);

        mockConfigManager.Setup(m => m.LoadAsync()).ReturnsAsync(asciiConfig);
        var asciiProvider = new SymbolProvider(mockConfigManager.Object);

        // Assert - Unicode and ASCII symbols should be different
        unicodeProvider.Error.Should().NotBe(asciiProvider.Error);
        unicodeProvider.Success.Should().NotBe(asciiProvider.Success);
        unicodeProvider.Music.Should().NotBe(asciiProvider.Music);
    }

    [Fact]
    public void SymbolProvider_AsciiSymbolsAreReadable()
    {
        // Arrange
        var mockConfigManager = new Mock<IConfigurationManager>();
        var config = new LfmConfig { UnicodeSymbols = UnicodeSupport.Disabled };
        mockConfigManager.Setup(m => m.LoadAsync()).ReturnsAsync(config);

        // Act
        var provider = new SymbolProvider(mockConfigManager.Object);

        // Assert - ASCII symbols should use brackets for clarity
        provider.Error.Should().Contain("[");
        provider.Success.Should().Contain("[");
        provider.Error.Should().Contain("]");
        provider.Success.Should().Contain("]");
    }

    // ========== Extended Unicode Symbol Tests ==========

    [Fact]
    public void SymbolProvider_Stats_ReturnsCorrectSymbol()
    {
        // Arrange
        var mockConfigManager = new Mock<IConfigurationManager>();
        var unicodeConfig = new LfmConfig { UnicodeSymbols = UnicodeSupport.Enabled };
        var asciiConfig = new LfmConfig { UnicodeSymbols = UnicodeSupport.Disabled };

        mockConfigManager.Setup(m => m.LoadAsync()).ReturnsAsync(unicodeConfig);
        var unicodeProvider = new SymbolProvider(mockConfigManager.Object);

        mockConfigManager.Setup(m => m.LoadAsync()).ReturnsAsync(asciiConfig);
        var asciiProvider = new SymbolProvider(mockConfigManager.Object);

        // Assert
        unicodeProvider.Stats.Should().Be("📊");
        asciiProvider.Stats.Should().Be("[STATS]");
    }

    [Fact]
    public void SymbolProvider_Settings_ReturnsCorrectSymbol()
    {
        // Arrange
        var mockConfigManager = new Mock<IConfigurationManager>();
        var unicodeConfig = new LfmConfig { UnicodeSymbols = UnicodeSupport.Enabled };
        var asciiConfig = new LfmConfig { UnicodeSymbols = UnicodeSupport.Disabled };

        mockConfigManager.Setup(m => m.LoadAsync()).ReturnsAsync(unicodeConfig);
        var unicodeProvider = new SymbolProvider(mockConfigManager.Object);

        mockConfigManager.Setup(m => m.LoadAsync()).ReturnsAsync(asciiConfig);
        var asciiProvider = new SymbolProvider(mockConfigManager.Object);

        // Assert
        unicodeProvider.Settings.Should().Be("⚙️");
        asciiProvider.Settings.Should().Be("[SETTINGS]");
    }

    [Fact]
    public void SymbolProvider_Cleanup_ReturnsCorrectSymbol()
    {
        // Arrange
        var mockConfigManager = new Mock<IConfigurationManager>();
        var unicodeConfig = new LfmConfig { UnicodeSymbols = UnicodeSupport.Enabled };
        var asciiConfig = new LfmConfig { UnicodeSymbols = UnicodeSupport.Disabled };

        mockConfigManager.Setup(m => m.LoadAsync()).ReturnsAsync(unicodeConfig);
        var unicodeProvider = new SymbolProvider(mockConfigManager.Object);

        mockConfigManager.Setup(m => m.LoadAsync()).ReturnsAsync(asciiConfig);
        var asciiProvider = new SymbolProvider(mockConfigManager.Object);

        // Assert
        unicodeProvider.Cleanup.Should().Be("🧹");
        asciiProvider.Cleanup.Should().Be("[CLEANUP]");
    }

    [Fact]
    public void SymbolProvider_Clipboard_ReturnsCorrectSymbol()
    {
        // Arrange
        var mockConfigManager = new Mock<IConfigurationManager>();
        var unicodeConfig = new LfmConfig { UnicodeSymbols = UnicodeSupport.Enabled };
        var asciiConfig = new LfmConfig { UnicodeSymbols = UnicodeSupport.Disabled };

        mockConfigManager.Setup(m => m.LoadAsync()).ReturnsAsync(unicodeConfig);
        var unicodeProvider = new SymbolProvider(mockConfigManager.Object);

        mockConfigManager.Setup(m => m.LoadAsync()).ReturnsAsync(asciiConfig);
        var asciiProvider = new SymbolProvider(mockConfigManager.Object);

        // Assert
        unicodeProvider.Clipboard.Should().Be("📋");
        asciiProvider.Clipboard.Should().Be("[LIST]");
    }

    [Fact]
    public void SymbolProvider_StopSign_ReturnsCorrectSymbol()
    {
        // Arrange
        var mockConfigManager = new Mock<IConfigurationManager>();
        var unicodeConfig = new LfmConfig { UnicodeSymbols = UnicodeSupport.Enabled };
        var asciiConfig = new LfmConfig { UnicodeSymbols = UnicodeSupport.Disabled };

        mockConfigManager.Setup(m => m.LoadAsync()).ReturnsAsync(unicodeConfig);
        var unicodeProvider = new SymbolProvider(mockConfigManager.Object);

        mockConfigManager.Setup(m => m.LoadAsync()).ReturnsAsync(asciiConfig);
        var asciiProvider = new SymbolProvider(mockConfigManager.Object);

        // Assert
        unicodeProvider.StopSign.Should().Be("🛑");
        asciiProvider.StopSign.Should().Be("[STOP]");
    }

    // ========== Auto-Detection Tests ==========

    [Fact]
    public void SymbolProvider_WithAutoDetection_SelectsBasedOnPlatform()
    {
        // Arrange
        var mockConfigManager = new Mock<IConfigurationManager>();
        var autoConfig = new LfmConfig { UnicodeSymbols = UnicodeSupport.Auto };
        mockConfigManager.Setup(m => m.LoadAsync()).ReturnsAsync(autoConfig);

        // Act
        var provider = new SymbolProvider(mockConfigManager.Object);

        // Assert - Should return a valid symbol (either emoji or ASCII format)
        provider.Error.Should().NotBeNullOrEmpty();
        // On Linux/macOS: should be emoji; on Windows: depends on terminal
        (provider.Error == "❌" || provider.Error == "[X]").Should().BeTrue();
    }

    [Fact]
    public void SymbolProvider_SymbolsAreConsistent_AcrossMultipleCalls()
    {
        // Arrange
        var mockConfigManager = new Mock<IConfigurationManager>();
        var config = new LfmConfig { UnicodeSymbols = UnicodeSupport.Enabled };
        mockConfigManager.Setup(m => m.LoadAsync()).ReturnsAsync(config);
        var provider = new SymbolProvider(mockConfigManager.Object);

        // Act
        var error1 = provider.Error;
        var error2 = provider.Error;
        var error3 = provider.Error;

        // Assert
        error1.Should().Be(error2);
        error2.Should().Be(error3);
    }
}

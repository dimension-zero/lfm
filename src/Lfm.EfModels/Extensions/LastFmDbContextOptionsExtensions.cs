using Lfm.Core.Services;
using Lfm.EfModels.Provider;
using Lfm.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Lfm.EfModels.Extensions;

/// <summary>
/// Extension methods for configuring LfmDbContext with music data providers.
/// </summary>
public static class LfmDbContextOptionsExtensions
{
    /// <summary>
    /// Configures the DbContext to use Last.fm API as the data source.
    /// </summary>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="apiClient">Last.fm API client (should be CachedLastFmApiClient for performance).</param>
    /// <param name="defaultUser">Default username when not specified in LINQ queries.</param>
    /// <returns>The options builder for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddDbContext&lt;LfmDbContext&gt;(options =>
    ///     options.UseLastFm(cachedApiClient, "smarshal"));
    /// </code>
    /// </example>
    public static DbContextOptionsBuilder<LfmDbContext> UseLastFm(
        this DbContextOptionsBuilder<LfmDbContext> optionsBuilder,
        ILastFmApiClient apiClient,
        string defaultUser)
    {
        if (optionsBuilder == null)
            throw new ArgumentNullException(nameof(optionsBuilder));
        if (apiClient == null)
            throw new ArgumentNullException(nameof(apiClient));
        if (string.IsNullOrWhiteSpace(defaultUser))
            throw new ArgumentException("Default user must be specified", nameof(defaultUser));

        // Wrap API client as IMusicDataProvider
        var dataProvider = new LastFmApiProvider(apiClient);

        // Store the provider configuration in the options extension
        var extension = optionsBuilder.Options.FindExtension<LfmOptionsExtension>()
            ?? new LfmOptionsExtension();

        extension = extension.WithDataProvider(dataProvider).WithDefaultUser(defaultUser);

        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

        return optionsBuilder;
    }

    /// <summary>
    /// Configures the DbContext to use Last.fm API as the data source (non-generic overload).
    /// </summary>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="apiClient">Last.fm API client (should be CachedLastFmApiClient for performance).</param>
    /// <param name="defaultUser">Default username when not specified in LINQ queries.</param>
    /// <returns>The options builder for chaining.</returns>
    public static DbContextOptionsBuilder UseLastFm(
        this DbContextOptionsBuilder optionsBuilder,
        ILastFmApiClient apiClient,
        string defaultUser)
    {
        if (optionsBuilder == null)
            throw new ArgumentNullException(nameof(optionsBuilder));
        if (apiClient == null)
            throw new ArgumentNullException(nameof(apiClient));
        if (string.IsNullOrWhiteSpace(defaultUser))
            throw new ArgumentException("Default user must be specified", nameof(defaultUser));

        // Wrap API client as IMusicDataProvider
        var dataProvider = new LastFmApiProvider(apiClient);

        var extension = optionsBuilder.Options.FindExtension<LfmOptionsExtension>()
            ?? new LfmOptionsExtension();

        extension = extension.WithDataProvider(dataProvider).WithDefaultUser(defaultUser);

        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

        return optionsBuilder;
    }

    /// <summary>
    /// Configures the DbContext to use local file data provider (Spotify/YouTube Music exports).
    /// </summary>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="dataProvider">Local file data provider instance</param>
    /// <param name="filePath">Path to data file (Spotify/YouTube export)</param>
    /// <returns>The options builder for chaining.</returns>
    /// <example>
    /// <code>
    /// var dataProvider = new LocalFileDataProvider(enrichmentService);
    /// services.AddDbContext&lt;LfmDbContext&gt;(options =>
    ///     options.UseLocalFiles(dataProvider, "C:\\spotify\\extended_streaming_history.json"));
    /// </code>
    /// </example>
    public static DbContextOptionsBuilder<LfmDbContext> UseLocalFiles(
        this DbContextOptionsBuilder<LfmDbContext> optionsBuilder,
        IMusicDataProvider dataProvider,
        string filePath)
    {
        if (optionsBuilder == null)
            throw new ArgumentNullException(nameof(optionsBuilder));
        if (dataProvider == null)
            throw new ArgumentNullException(nameof(dataProvider));
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path must be specified", nameof(filePath));

        // Store the provider configuration in the options extension
        var extension = optionsBuilder.Options.FindExtension<LfmOptionsExtension>()
            ?? new LfmOptionsExtension();

        extension = extension.WithDataProvider(dataProvider).WithDefaultUser(filePath);

        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

        return optionsBuilder;
    }

    /// <summary>
    /// Configures the DbContext to use local file data provider (non-generic overload).
    /// </summary>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="dataProvider">Local file data provider instance</param>
    /// <param name="filePath">Path to data file (Spotify/YouTube export)</param>
    /// <returns>The options builder for chaining.</returns>
    public static DbContextOptionsBuilder UseLocalFiles(
        this DbContextOptionsBuilder optionsBuilder,
        IMusicDataProvider dataProvider,
        string filePath)
    {
        if (optionsBuilder == null)
            throw new ArgumentNullException(nameof(optionsBuilder));
        if (dataProvider == null)
            throw new ArgumentNullException(nameof(dataProvider));
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path must be specified", nameof(filePath));

        var extension = optionsBuilder.Options.FindExtension<LfmOptionsExtension>()
            ?? new LfmOptionsExtension();

        extension = extension.WithDataProvider(dataProvider).WithDefaultUser(filePath);

        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

        return optionsBuilder;
    }
}

/// <summary>
/// EF Core options extension for music data provider configuration.
/// </summary>
public class LfmOptionsExtension : IDbContextOptionsExtension
{
    private IMusicDataProvider? _dataProvider;
    private string? _defaultUser;

    public IMusicDataProvider? DataProvider => _dataProvider;
    public string? DefaultUser => _defaultUser;

    /// <summary>
    /// Gets information/metadata about the extension.
    /// </summary>
    public DbContextOptionsExtensionInfo Info => new LfmExtensionInfo(this);

    /// <summary>
    /// Create a copy of the extension with a data provider.
    /// </summary>
    public LfmOptionsExtension WithDataProvider(IMusicDataProvider dataProvider)
    {
        var clone = Clone();
        clone._dataProvider = dataProvider;
        return clone;
    }

    /// <summary>
    /// Create a copy of the extension with a default user.
    /// </summary>
    public LfmOptionsExtension WithDefaultUser(string defaultUser)
    {
        var clone = Clone();
        clone._defaultUser = defaultUser;
        return clone;
    }

    /// <summary>
    /// Clone the extension (required for EF Core immutability pattern).
    /// </summary>
    private LfmOptionsExtension Clone()
    {
        return new LfmOptionsExtension
        {
            _dataProvider = _dataProvider,
            _defaultUser = _defaultUser
        };
    }

    /// <summary>
    /// Apply services to the service collection (not used for this provider).
    /// </summary>
    public void ApplyServices(IServiceCollection services)
    {
        // Data provider doesn't require additional services
        // Provider and user/file path are provided directly
    }

    /// <summary>
    /// Validate the extension configuration.
    /// </summary>
    public void Validate(IDbContextOptions options)
    {
        if (_dataProvider == null)
            throw new InvalidOperationException("Music data provider must be configured. Call UseLastFm(), UseSpotifyFiles(), or UseYouTubeFiles().");

        if (string.IsNullOrWhiteSpace(_defaultUser))
            throw new InvalidOperationException("Default user/file path must be configured.");
    }
}

/// <summary>
/// Extension information for EF Core diagnostics.
/// </summary>
public class LfmExtensionInfo : DbContextOptionsExtensionInfo
{
    private readonly LfmOptionsExtension _extension;

    public LfmExtensionInfo(LfmOptionsExtension extension)
        : base(extension)
    {
        _extension = extension;
    }

    /// <summary>
    /// Extension is not a database provider (it's a query provider over music data sources).
    /// </summary>
    public override bool IsDatabaseProvider => false;

    /// <summary>
    /// Log fragment for diagnostics.
    /// </summary>
    public override string LogFragment =>
        $"Lfm(Provider={_extension.DataProvider?.ProviderName ?? "not set"}, User={_extension.DefaultUser ?? "not set"})";

    /// <summary>
    /// Calculate hash code for caching.
    /// </summary>
    public override int GetServiceProviderHashCode()
    {
        return HashCode.Combine(_extension.DataProvider, _extension.DefaultUser);
    }

    /// <summary>
    /// Check if service provider configuration should be cached.
    /// </summary>
    public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
    {
        return other is LfmExtensionInfo otherInfo &&
               _extension.DataProvider == otherInfo._extension.DataProvider &&
               _extension.DefaultUser == otherInfo._extension.DefaultUser;
    }

    /// <summary>
    /// Populate debug info for diagnostics.
    /// </summary>
    public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
    {
        debugInfo["Lfm:DefaultUser"] = _extension.DefaultUser ?? "not set";
        debugInfo["Lfm:ProviderType"] = _extension.DataProvider?.GetType().Name ?? "not set";
        debugInfo["Lfm:ProviderName"] = _extension.DataProvider?.ProviderName ?? "not set";
    }
}

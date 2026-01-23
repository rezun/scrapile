namespace Scrapile.Desktop.DependencyInjection;

using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Scrapile.Application.Services;
using Scrapile.Domain.Interfaces;
using Scrapile.Infrastructure.Repositories;
using Scrapile.Infrastructure.Storage;
using Scrapile.Desktop.Services;
using Scrapile.Desktop.ViewModels;

/// <summary>
/// Extension methods for configuring dependency injection services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures all application services for dependency injection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="storageDirectory">The directory for document storage.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddScrapileServices(this IServiceCollection services, string storageDirectory)
    {
        // Infrastructure - Singletons for shared state
        services.AddSingleton<ISettingsStore, JsonSettingsStore>();
        services.AddSingleton<IMetadataStore>(sp => new JsonMetadataStore(storageDirectory));
        services.AddSingleton<IDocumentRepository>(sp =>
        {
            var metadataStore = sp.GetRequiredService<IMetadataStore>();
            return new FileSystemDocumentRepository(storageDirectory, metadataStore);
        });

        // Application Services - Singletons for shared state
        services.AddSingleton<SettingsService>();
        services.AddSingleton<DocumentService>();
        services.AddSingleton<AutoSaveService>();
        services.AddSingleton<TabManager>();

        // Desktop Services - Singletons for shared state
        services.AddSingleton<ThemeService>();

        // ViewModels - Transient for fresh instances
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<SettingsViewModel>();

        return services;
    }

    /// <summary>
    /// Gets the default storage directory for the application.
    /// Uses a platform-appropriate location in the user's documents folder.
    /// </summary>
    /// <returns>The default storage directory path.</returns>
    public static string GetDefaultStorageDirectory()
    {
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var scrapilePath = Path.Combine(documentsPath, "Scrapile");
        return scrapilePath;
    }
}

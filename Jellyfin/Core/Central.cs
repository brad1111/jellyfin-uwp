using System;

namespace Jellyfin.Core;

/// <summary>
/// Provides access to core application services and managers.
/// </summary>
public static class Central
{
    /// <summary>
    /// Gets the settings manager for application configuration.
    /// </summary>
    public static SettingsManager Settings { get; } = new SettingsManager();

    /// <summary>
    /// Gets the minimum supported Jellyfin server version supported on this client.
    /// </summary>
    public static Version MinimumSupportedServerVersion { get; } = new(10, 11, 0);
}

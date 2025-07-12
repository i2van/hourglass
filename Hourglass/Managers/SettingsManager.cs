// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SettingsManager.cs" company="Chris Dziemborowicz">
//   Copyright (c) Chris Dziemborowicz. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Hourglass.Managers;

using Properties;

// ReSharper disable ExceptionNotDocumented

/// <summary>
/// Manages default settings.
/// </summary>
public sealed class SettingsManager : Manager
{
    /// <summary>
    /// Singleton instance of the <see cref="SettingsManager"/> class.
    /// </summary>
    public static readonly SettingsManager Instance = new();

    /// <summary>
    /// Prevents a default instance of the <see cref="SettingsManager"/> class from being created.
    /// </summary>
    private SettingsManager()
    {
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        if (Settings.Default.UpgradeRequired)
        {
            Settings.Default.Upgrade();
            Settings.Default.UpgradeRequired = false;
            Settings.Default.Save();
        }
    }

    /// <inheritdoc />
    public override void Persist()
    {
        Settings.Default.Save();
    }
}
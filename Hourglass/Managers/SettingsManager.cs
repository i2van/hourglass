// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SettingsManager.cs" company="Chris Dziemborowicz">
//   Copyright (c) Chris Dziemborowicz. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Hourglass.Managers;

using System;
#if PORTABLE
using System.IO;
using System.Reflection;
#else
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
#endif

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
#if !PORTABLE
        TryPreRepairConfigFile();

        try
        {
            InitializeSettings();
        }
        catch (ConfigurationErrorsException ex)
        {
            // Preemptive repair didn't help or wasn't needed; try reactive repair.
            TryRepairConfigFile(ex);
            ResetConfigurationSystem();

            try
            {
                InitializeSettings();
            }
            catch (ConfigurationErrorsException)
            {
                // Recovery failed, app will use default settings.
            }
        }
#else
        InitializeSettings();
#endif
    }

    private static void InitializeSettings()
    {
        if (Settings.Default.UpgradeRequired)
        {
            Settings.Default.Upgrade();
            Settings.Default.UpgradeRequired = false;
            TrySave();
        }
    }

#if !PORTABLE
    /// <summary>
    /// Attempts to detect and repair a broken config file before the configuration system is initialized.
    /// Searches for the user.config file directly without using ConfigurationManager APIs to avoid poisoning the system.
    /// </summary>
    private static void TryPreRepairConfigFile()
    {
        try
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string searchRoot = Path.Combine(localAppData, "Chris_Dziemborowicz,_Ivan");

            if (!Directory.Exists(searchRoot))
            {
                return;
            }

            foreach (string configFile in Directory.GetFiles(searchRoot, "user.config", SearchOption.AllDirectories))
            {
                TryRepairSingleConfigFile(configFile);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Ignore errors during preemptive repair.
        }
    }

    /// <summary>
    /// Checks a single config file and adds the <c>configSections</c> element if missing.
    /// </summary>
    private static void TryRepairSingleConfigFile(string filePath)
    {
        try
        {
            XDocument doc = XDocument.Load(filePath);
            XElement? configuration = doc.Element("configuration");
            if (configuration is null)
            {
                return;
            }

            if (configuration.Element("configSections") is not null)
            {
                return;
            }

            if (configuration.Element("userSettings") is null)
            {
                return;
            }

            configuration.AddFirst(CreateConfigSectionsElement());
            doc.Save(filePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Xml.XmlException)
        {
            // Ignore errors for individual files.
        }
    }

    /// <summary>
    /// Creates the <c>configSections</c> element with the required section group declaration.
    /// </summary>
    private static XElement CreateConfigSectionsElement() =>
        new("configSections",
            new XElement("sectionGroup",
                new XAttribute("name", "userSettings"),
                new XAttribute("type", "System.Configuration.UserSettingsGroup, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"),
                new XElement("section",
                    new XAttribute("name", "Hourglass.Properties.Settings"),
                    new XAttribute("type", "System.Configuration.ClientSettingsSection, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"),
                    new XAttribute("allowExeDefinition", "MachineToLocalUser"),
                    new XAttribute("requirePermission", "false"))));

    /// <summary>
    /// Tries to repair a broken config file by adding the missing <c>configSections</c> element and reloading the configuration.
    /// </summary>
    /// <param name="ex">The configuration exception that was thrown.</param>
    /// <returns><c>true</c> if the config file was repaired successfully; otherwise, <c>false</c>.</returns>
    private static bool TryRepairConfigFile(ConfigurationErrorsException ex)
    {
        string? filePath = GetConfigFilePath(ex);
        if (filePath is null || !File.Exists(filePath))
        {
            return false;
        }

        try
        {
            XDocument doc = XDocument.Load(filePath);
            XElement? configuration = doc.Element("configuration");
            if (configuration is null)
            {
                return false;
            }

            if (configuration.Element("configSections") is not null)
            {
                return false;
            }

            configuration.AddFirst(CreateConfigSectionsElement());
            doc.Save(filePath);

            return true;
        }
        catch (Exception repairEx) when (repairEx is IOException or UnauthorizedAccessException or System.Xml.XmlException)
        {
            return false;
        }
    }

    /// <summary>
    /// Resets the internal configuration system state so it re-reads config files on next access.
    /// </summary>
    private static void ResetConfigurationSystem()
    {
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;

        var configManagerType = typeof(ConfigurationManager);

        configManagerType
            .GetField("s_initState", flags)
            ?.SetValue(null, 0);

        configManagerType
            .GetField("s_initError", flags)
            ?.SetValue(null, null);

        configManagerType
            .GetField("s_configSystem", flags)
            ?.SetValue(null, null);

        var clientConfigType = configManagerType.Assembly
            .GetType("System.Configuration.ClientConfigurationSystem");

        if (clientConfigType is not null)
        {
            foreach (string fieldName in new[] { "s_current", "s_instance", "_configSystem" })
            {
                clientConfigType.GetField(fieldName, flags)?.SetValue(null, null);
            }
        }
    }

    /// <summary>
    /// Gets the config file path from a <see cref="ConfigurationErrorsException"/>.
    /// </summary>
    /// <param name="ex">The configuration exception.</param>
    /// <returns>The config file path, or <c>null</c> if it could not be determined.</returns>
    private static string? GetConfigFilePath(ConfigurationErrorsException ex)
    {
        if (!string.IsNullOrEmpty(ex.Filename))
        {
            return ex.Filename;
        }

        if (ex.InnerException is ConfigurationErrorsException inner && !string.IsNullOrEmpty(inner.Filename))
        {
            return inner.Filename;
        }

        try
        {
            return GetSettingsFilePath();
        }
        catch
        {
            return null;
        }
    }
#endif

    /// <inheritdoc />
    public override void Persist()
    {
        TrySave();
    }

    private static void TrySave()
    {
        try
        {
            Settings.Default.Save();
        }
        catch (UnauthorizedAccessException) when (Settings.Default.IgnoreSettingsWriteErrors)
        {
            // Ignore errors when the settings file is read-only.
        }
    }

    /// <summary>
    /// Returns the path to the settings file.
    /// </summary>
    /// <returns>The path to the settings file.</returns>
    public static string GetSettingsFilePath()
    {
#if PORTABLE
        string location = Assembly.GetExecutingAssembly().Location;
        return Path.Combine(Path.GetDirectoryName(location) ?? @".\", Path.ChangeExtension(Path.GetFileName(location), ".config"));
#else
        return ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;
#endif
    }
}
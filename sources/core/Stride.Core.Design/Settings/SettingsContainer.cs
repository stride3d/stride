// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using Stride.Core.Annotations;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Yaml.Events;

namespace Stride.Core.Settings;

/// <summary>
/// A container object that contains a collection of <see cref="SettingsKey"/>. Each settings key can store a corresponding value into a <see cref="SettingsProfile"/>.
/// When a <see cref="SettingsContainer"/> is created, it will contain a default root <see cref="SettingsProfile"/>. This profile has no parent, and every profile created
/// or loaded afterward will have the default profile as parent, unless another non-null parent is specified.
/// </summary>
public class SettingsContainer
{
    internal static readonly object SettingsLock = new();

    /// <summary>
    /// A dictionary containing every existing <see cref="SettingsKey"/>.
    /// </summary>
    private readonly Dictionary<UFile, SettingsKey> settingsKeys = [];

    /// <summary>
    /// A list containing every <see cref="SettingsProfile"/> registered in the <see cref="SettingsContainer"/>.
    /// </summary>
    private readonly List<SettingsProfile> profileList = [];

    /// <summary>
    /// The settings profile that is currently active.
    /// </summary>
    private SettingsProfile currentProfile;

    public SettingsContainer()
    {
        RootProfile = new SettingsProfile(this, null);
        profileList.Add(RootProfile);
        currentProfile = RootProfile;
        Logger = new LoggerResult();
    }

    /// <summary>
    /// Gets the logger associated to the <see cref="SettingsContainer"/>.
    /// </summary>
    public LoggerResult Logger { get; }

    /// <summary>
    /// Gets the root profile of this settings container.
    /// </summary>
    /// <remarks>
    /// The root profile is a <see cref="SettingsProfile"/> that contains the default value of all registered <see cref="SettingsKey"/>.
    /// </remarks>
    public SettingsProfile RootProfile { get; }

    /// <summary>
    /// Gets or sets the <see cref="SettingsProfile"/> that is currently active.
    /// </summary>
    public SettingsProfile CurrentProfile { get { return currentProfile; } set { ChangeCurrentProfile(currentProfile, value); } }

    /// <summary>
    /// Gets the list of registered profiles.
    /// </summary>
    internal IEnumerable<SettingsProfile> Profiles => profileList;

    /// <summary>
    /// Raised when a settings file has been loaded.
    /// </summary>
    public event EventHandler<SettingsFileLoadedEventArgs> SettingsFileLoaded;

    /// <summary>
    /// Gets a list of all registered <see cref="SettingsKey"/> instances.
    /// </summary>
    /// <returns>A list of all registered <see cref="SettingsKey"/> instances.</returns>
    [NotNull]
    public List<SettingsKey> GetAllSettingsKeys()
    {
        return [.. settingsKeys.Values];
    }

    /// <summary>
    /// Creates a new settings profile.
    /// </summary>
    /// <param name="setAsCurrent">If <c>true</c>, the created profile will also be set as <see cref="CurrentProfile"/>.</param>
    /// <param name="parent">The parent profile of the settings to create. If <c>null</c>, the default profile will be used.</param>
    /// <param name="registerInContainer">If true, the profile will be registered in this container. Otherwise it will be disconnected from the container.</param>
    /// <returns>A new instance of the <see cref="SettingsProfile"/> class.</returns>
    /// <remarks>
    /// If the profile is not registered to the container, it won't be able to receive <see cref="SettingsKey"/> that are registered after its
    /// creation. If the profile is registered to the container, <see cref="UnloadSettingsProfile"/> must be call in order to unregister it.
    /// </remarks>
    [NotNull]
    public SettingsProfile CreateSettingsProfile(bool setAsCurrent, SettingsProfile? parent = null, bool registerInContainer = true)
    {
        if (setAsCurrent && !registerInContainer) throw new ArgumentException("Cannot set the profile as current if it's not registered to the container", nameof(setAsCurrent));

        var profile = new SettingsProfile(this, parent ?? RootProfile);

        if (registerInContainer)
        {
            lock (SettingsLock)
            {
                profileList.Add(profile);
                if (setAsCurrent)
                    CurrentProfile = profile;
            }
        }
        return profile;
    }

    /// <summary>
    /// Loads a settings profile from the given file.
    /// </summary>
    /// <param name="filePath">The path of the file from which to load settings.</param>
    /// <param name="setAsCurrent">If <c>true</c>, the loaded profile will also be set as <see cref="CurrentProfile"/>.</param>
    /// <param name="parent">The profile to use as parent for the loaded profile. If <c>null</c>, a default profile will be used.</param>
    /// <param name="registerInContainer">If true, the profile will be registered in this container. Otherwise it will be disconnected from the container.</param>
    /// <returns><c>true</c> if settings were correctly loaded, <c>false</c> otherwise.</returns>
    /// <remarks>
    /// If the profile is not registered to the container, it won't be able to receive <see cref="SettingsKey"/> that are registered after its
    /// creation. If the profile is registered to the container, <see cref="UnloadSettingsProfile"/> must be call in order to unregister it.
    /// </remarks>
    public SettingsProfile? LoadSettingsProfile(UFile filePath, bool setAsCurrent, SettingsProfile? parent = null, bool registerInContainer = true)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(filePath);
#else
        if (filePath is null) throw new ArgumentNullException(nameof(filePath));
#endif
        if (setAsCurrent && !registerInContainer) throw new ArgumentException("Cannot set the profile as current if it's not registered to the container", nameof(setAsCurrent));

        if (!File.Exists(filePath))
        {
            Logger.Error($"Settings file [{filePath}] was not found");
            return null;
        }

        var profile = new SettingsProfile(this, parent ?? RootProfile) { FilePath = filePath };
        try
        {
            var settingsFile = new SettingsFile(profile);
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            SettingsYamlSerializer.Default.Deserialize(stream, settingsFile);
        }
        catch (Exception)
        {
            return null;
        }

        if (registerInContainer)
        {
            lock (SettingsLock)
            {
                profileList.Add(profile);
                if (setAsCurrent)
                {
                    CurrentProfile = profile;
                }
            }
        }

        var handler = SettingsFileLoaded;
        handler?.Invoke(null, new SettingsFileLoadedEventArgs(filePath));
        return profile;
    }

    /// <summary>
    /// Reloads a profile from its file, updating the value that have changed.
    /// </summary>
    /// <param name="profile">The profile to reload.</param>
    public void ReloadSettingsProfile(SettingsProfile profile)
    {
        var filePath = profile.FilePath;
        if (filePath == null) throw new ArgumentException(null, nameof(profile));
        if (!File.Exists(filePath))
        {
            Logger.Error($"Settings file [{filePath}] was not found");
            throw new ArgumentException(null, nameof(profile));
        }

        try
        {
            var settingsFile = new SettingsFile(profile);
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            SettingsYamlSerializer.Default.Deserialize(stream, settingsFile);
        }
        catch (Exception e)
        {
            Logger.Error($"Error while loading settings file [{filePath}].", e);
        }

        var handler = SettingsFileLoaded;
        handler?.Invoke(null, new SettingsFileLoadedEventArgs(filePath));
    }

    /// <summary>
    /// Unloads a profile that was previously loaded.
    /// </summary>
    /// <param name="profile">The profile to unload.</param>
    public void UnloadSettingsProfile(SettingsProfile profile)
    {
        if (profile == RootProfile)
            throw new ArgumentException("The default profile cannot be unloaded");
        if (profile == CurrentProfile)
            throw new InvalidOperationException("Unable to unload the current profile.");
        lock (SettingsLock)
        {
            profileList.Remove(profile);
        }
    }

    /// <summary>
    /// Saves the given settings profile to a file at the given path.
    /// </summary>
    /// <param name="profile">The profile to save.</param>
    /// <param name="filePath">The path of the file.</param>
    /// <returns><c>true</c> if the file was correctly saved, <c>false</c> otherwise.</returns>
    public bool SaveSettingsProfile(SettingsProfile profile, UFile filePath)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(profile);
#else
        if (profile is null) throw new ArgumentNullException(nameof(profile));
#endif
        try
        {
            profile.Saving = true;
            Directory.CreateDirectory(filePath.GetFullDirectory());

            var settingsFile = new SettingsFile(profile);
            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                SettingsYamlSerializer.Default.Serialize(stream, settingsFile);
            }

            if (filePath != profile.FilePath)
            {
                if (File.Exists(profile.FilePath))
                {
                    File.Delete(profile.FilePath);
                }

                profile.FilePath = filePath;
            }
        }
        catch (Exception e)
        {
            Logger.Error($"Error while saving settings file [{filePath}]", e);
            return false;
        }
        finally
        {
            profile.Saving = false;
        }
        return true;
    }

    internal void EncodeSettings(SettingsProfile profile, SettingsDictionary settingsDictionary)
    {
        lock (SettingsLock)
        {
            foreach (var entry in profile.Settings.Values)
            {
                try
                {
                    // Find key
                    settingsKeys.TryGetValue(entry.Name, out var key);
                    settingsDictionary.Add(entry.Name, entry.GetSerializableValue(key));
                }
                catch (Exception e)
                {
                    e.Ignore();
                }
            }
        }
    }

    internal void DecodeSettings(SettingsDictionary settingsDictionary, SettingsProfile profile)
    {
        lock (SettingsLock)
        {
            foreach (var settings in settingsDictionary)
            {
                var value = settings.Value;
                object finalValue = value;
                if (settingsKeys.TryGetValue(settings.Key, out var key))
                {
                    finalValue = key.ConvertValue(value);
                }
                profile.SetValue(settings.Key, finalValue);
            }
        }
    }

    /// <summary>
    /// Gets the settings key that matches the given name.
    /// </summary>
    /// <param name="name">The name of the settings property to fetch.</param>
    /// <returns>The settings key that matches the given name, or <c>null</c>.</returns>
    public SettingsKey? GetSettingsKey(UFile name)
    {
        lock (SettingsLock)
        {
            settingsKeys.TryGetValue(name, out var key);
            return key;
        }
    }

    /// <summary>
    /// Clears the current settings, by removing registered <see cref="SettingsKey"/> and <see cref="SettingsProfile"/> instances. This method should be used only for tests.
    /// </summary>
    public void ClearSettings()
    {
        lock (SettingsLock)
        {
            CurrentProfile = RootProfile;
            CurrentProfile.ValidateSettingsChanges();
            profileList.Clear();
            RootProfile.Settings.Clear();
            settingsKeys.Clear();
        }
    }

    internal void RegisterSettingsKey(UFile name, object defaultValue, SettingsKey settingsKey)
    {
        lock (SettingsLock)
        {
            settingsKeys.Add(name, settingsKey);
            var entry = SettingsEntry.CreateFromValue(RootProfile, name, defaultValue);
            RootProfile.RegisterEntry(entry);

            // Ensure that the value is converted to the key type in each loaded profile.
            foreach (var profile in Profiles.Where(x => x != RootProfile))
            {
                if (profile.Settings.TryGetValue(name, out entry))
                {
                    var convertedValue = entry.Value is List<ParsingEvent> parsingEvents ? settingsKey.ConvertValue(parsingEvents) : entry.Value;
                    entry = SettingsEntry.CreateFromValue(profile, name, convertedValue);
                    profile.Settings[name] = entry;
                }
            }
        }
    }

    private void ChangeCurrentProfile(SettingsProfile oldProfile, SettingsProfile newProfile)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(oldProfile);
        ArgumentNullException.ThrowIfNull(newProfile);
#else
        if (oldProfile is null) throw new ArgumentNullException(nameof(oldProfile));
        if (newProfile is null) throw new ArgumentNullException(nameof(newProfile));
#endif
        currentProfile = newProfile;

        lock (SettingsLock)
        {
            foreach (var key in settingsKeys)
            {
                oldProfile.GetValue(key.Key, out var oldValue, true, false);
                newProfile.GetValue(key.Key, out var newValue, true, false);
                bool isDifferent;
                if (oldValue is IList oldList && newValue is IList newList)
                {
                    isDifferent = oldList.Count != newList.Count;
                    for (int i = 0; i < oldList.Count && !isDifferent; ++i)
                    {
                        if (!Equals(oldList[i], newList[i]))
                            isDifferent = true;
                    }
                }
                else if (oldValue is IDictionary oldDictionary && newValue is IDictionary newDictionary)
                {
                    isDifferent = oldDictionary.Count != newDictionary.Count;
                    foreach (var k in oldDictionary.Keys)
                    {
                        if (!newDictionary.Contains(k) || !Equals(oldDictionary[k], newDictionary[k]))
                            isDifferent = true;
                    }
                }
                else
                {
                    isDifferent = !Equals(oldValue, newValue);
                }
                if (isDifferent)
                {
                    newProfile.NotifyEntryChanged(key.Key);
                }
            }
        }

        // Changes have been notified, empty the list of modified settings.
        newProfile.ValidateSettingsChanges();
    }
}

// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.MostRecentlyUsedFiles;
using Stride.Core.Settings;
using Stride.Core.Yaml;

namespace Stride.Launcher.Services;

public static class GameStudioSettings
{
    private static readonly SettingsProfile GameStudioProfile;

    private static readonly SettingsContainer InternalSettingsContainer = new();

    private static readonly SettingsContainer GameStudioSettingsContainer = new();

    private static readonly SettingsKey<MRUDictionary> MostRecentlyUsedSessionsKey = new("Internal/MostRecentlyUsedSessions", InternalSettingsContainer, () => new MRUDictionary());

    private static readonly SettingsKey<string> StoreCrashEmail = new("Interface/StoreCrashEmail", GameStudioSettingsContainer, "");

    private static readonly object LockObject = new();

    private static readonly MostRecentlyUsedFileCollection MRU;

    private static IReadOnlyCollection<UFile>? mostRecentlyUsed;

    private static bool updating;

    private static Timer? reloadTimer;

    static GameStudioSettings()
    {
        MRU = new MostRecentlyUsedFileCollection(LoadLatestInternalProfile, MostRecentlyUsedSessionsKey, () => InternalSettingsContainer.SaveSettingsProfile(InternalSettingsContainer.CurrentProfile, GetLatestInternalConfigPath()));
        MostRecentlyUsedSessionsKey.FallbackDeserializers.Add(LegacyMRUDeserializer);
        InternalSettingsContainer.LoadSettingsProfile(GetLatestInternalConfigPath(), true);
        InternalSettingsContainer.CurrentProfile.MonitorFileModification = true;
        InternalSettingsContainer.CurrentProfile.FileModified += GameStudioSettingsFileChanged;
        GameStudioProfile = GameStudioSettingsContainer.LoadSettingsProfile(GetLatestGameStudioConfigPath(), true);
        UpdateMostRecentlyUsed();
    }

    public static event EventHandler<EventArgs>? RecentProjectsUpdated;

    public static string CrashReportEmail
    {
        get
        {
            try
            {
                lock (LockObject)
                {
                    GameStudioSettingsContainer.ReloadSettingsProfile(GameStudioProfile);
                    return StoreCrashEmail.GetValue();
                }
            }
            catch (Exception)
            {
                return "";
            }
        }
        set
        {
            try
            {
                lock (LockObject)
                {
                    GameStudioSettingsContainer.ReloadSettingsProfile(GameStudioProfile);
                    StoreCrashEmail.SetValue(value);
                    GameStudioSettingsContainer.SaveSettingsProfile(GameStudioProfile, GetLatestGameStudioConfigPath());
                }
            }
            catch (Exception e)
            {
                e.Ignore();
            }
        }
    }

    public static IReadOnlyCollection<UFile> GetMostRecentlyUsed()
    {
        List<UFile> result;
        lock (LockObject)
        {
            result = new(mostRecentlyUsed ?? Enumerable.Empty<UFile>());
        }
        return result;
    }

    private static void GameStudioSettingsFileChanged(object? sender, FileModifiedEventArgs e)
    {
        // A change is seen as Game Studio starts writing the file (locked until it's done), and a save raises several:
        // reload once when they stop
        lock (LockObject)
        {
            reloadTimer ??= new Timer(_ => ReloadChangedFile());
            reloadTimer.Change(200, Timeout.Infinite);
        }
    }

    private static void ReloadChangedFile()
    {
        try
        {
            InternalSettingsContainer.ReloadSettingsProfile(InternalSettingsContainer.CurrentProfile);
        }
        catch (Exception e)
        {
            // The file was deleted: the recent projects are read from the defaults
            e.Ignore();
        }
        UpdateMostRecentlyUsed();
    }

    public static void RemoveMostRecentlyUsed(UFile filePath, string strideVersion)
    {
        lock (LockObject)
        {
            MRU.RemoveFile(filePath, strideVersion);
            UpdateMostRecentlyUsed();
        }
    }

    private static void UpdateMostRecentlyUsed()
    {
        if (updating)
            return;

        lock (LockObject)
        {
            updating = true;
            MRU.LoadFromSettings();
            updating = false;
            mostRecentlyUsed = MRU.MostRecentlyUsedFiles.Select(x => x.FilePath).ToList();
        }
        RecentProjectsUpdated?.Invoke(null, EventArgs.Empty);
    }

    // The profile loaded last when the file still can't be read
    private static SettingsProfile LoadLatestInternalProfile()
    {
        // Game Studio locks the file while it writes it: a failed read would empty the recent projects until the next
        // change, so retry for a moment
        for (var attempt = 0; ; ++attempt)
        {
            var path = GetLatestInternalConfigPath();
            var profile = InternalSettingsContainer.LoadSettingsProfile(path, false, null, false);
            if (profile is not null || attempt == 10 || !File.Exists(path))
                return profile ?? InternalSettingsContainer.CurrentProfile;
            Thread.Sleep(50);
        }
    }

    private static string GetLatestInternalConfigPath()
    {
        return GetInternalConfigPaths().FirstOrDefault(File.Exists) ?? EditorPath.InternalConfigPath;
    }

    private static string GetLatestGameStudioConfigPath()
    {
        return GetGameStudioConfigPaths().FirstOrDefault(File.Exists) ?? EditorPath.EditorConfigPath;
    }

    private static IEnumerable<string> GetInternalConfigPaths()
    {
        yield return EditorPath.InternalConfigPath;
    }

    private static IEnumerable<string> GetGameStudioConfigPaths()
    {
        yield return EditorPath.EditorConfigPath;
    }

    private static object LegacyMRUDeserializer(EventReader eventReader)
    {
        const string legacyVersion = "1.3";
        var mru = (List<UFile>)SettingsYamlSerializer.Default.Deserialize(eventReader, typeof(List<UFile>));
        var initialTimestamp = DateTime.UtcNow.Ticks;
        return new Dictionary<string, List<MostRecentlyUsedFile>>
        {
            { legacyVersion, mru.Select(x => new MostRecentlyUsedFile(x) { Timestamp = initialTimestamp-- }).ToList() }
        };
    }
}

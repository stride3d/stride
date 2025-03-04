// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Extensions;
using Stride.Core.Settings;

namespace Stride.Core.Assets;

/// <summary>
/// A class representing the user settings related to a <see cref="Package"/>. These settings are stored in a .user
/// file along the package file.
/// </summary>
public class PackageUserSettings
{
    private const string SettingsExtension = ".user";
    private readonly Package package;

    public static SettingsContainer SettingsContainer = new();

    internal PackageUserSettings(Package package)
    {
        ArgumentNullException.ThrowIfNull(package);
        this.package = package;
        if (package.FullPath == null)
        {
            Profile = SettingsContainer.CreateSettingsProfile(false);
        }
        else
        {
            var path = package.FullPath + SettingsExtension;
            SettingsProfile? profile = null;
            try
            {
                profile = SettingsContainer.LoadSettingsProfile(path, false);
            }
            catch (Exception e)
            {
                e.Ignore();
            }
            Profile = profile ?? SettingsContainer.CreateSettingsProfile(false);
        }
    }

    public bool Save()
    {
        if (package.FullPath == null)
            return false;

        var path = package.FullPath + SettingsExtension;
        return SettingsContainer.SaveSettingsProfile(Profile, path);
    }

    public SettingsProfile Profile { get; }

    public T GetValue<T>(SettingsKey<T> key)
    {
        return key.GetValue(Profile, true);
    }

    public void SetValue<T>(SettingsKey<T> key, T value)
    {
        key.SetValue(value, Profile);
    }
}

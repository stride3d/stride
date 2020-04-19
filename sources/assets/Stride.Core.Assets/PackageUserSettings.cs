// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;

using Stride.Core.Extensions;
using Stride.Core.Settings;

namespace Stride.Core.Assets
{
    /// <summary>
    /// A class representing the user settings related to a <see cref="Package"/>. These settings are stored in a .user
    /// file along the package file.
    /// </summary>
    public class PackageUserSettings
    {
        private const string SettingsExtension = ".user";
        private readonly Package package;
        private readonly SettingsProfile profile;

        public static SettingsContainer SettingsContainer = new SettingsContainer();

        internal PackageUserSettings(Package package)
        {
            if (package == null) throw new ArgumentNullException("package");
            this.package = package;
            if (package.FullPath == null)
            {
                profile = SettingsContainer.CreateSettingsProfile(false);
            }
            else
            {
                var path = package.FullPath + SettingsExtension;
                try
                {
                    profile = SettingsContainer.LoadSettingsProfile(path, false);
                }
                catch (Exception e)
                {
                    e.Ignore();
                }
                if (profile == null)
                    profile = SettingsContainer.CreateSettingsProfile(false);
            }
        }

        public bool Save()
        {
            if (package.FullPath == null)
                return false;

            var path = package.FullPath + SettingsExtension;
            return SettingsContainer.SaveSettingsProfile(profile, path);
        }

        public SettingsProfile Profile { get { return profile; } }

        public T GetValue<T>(SettingsKey<T> key)
        {
            return key.GetValue(profile, true);
        }

        public void SetValue<T>(SettingsKey<T> key, T value)
        {
            key.SetValue(value, profile);
        }
    }
}

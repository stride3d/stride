// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Settings;

namespace Stride.Core.Assets.Editor.Components.Properties
{
    public struct PackageSettingsEntry
    {
        public SettingsKey SettingsKey;
        public TargetPackage TargetPackage;

        public PackageSettingsEntry(SettingsKey key, TargetPackage targetPackage = TargetPackage.All)
        {
            SettingsKey = key;
            TargetPackage = targetPackage;
        }
    }
}

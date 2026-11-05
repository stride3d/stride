// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable
using Stride.Core.Settings;

namespace Stride.Core.Assets.Presentation.Components.Properties
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

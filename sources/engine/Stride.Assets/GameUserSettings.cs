// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Stride.Core.Settings;
using Stride.Engine.Design;

namespace Stride.Assets
{
    public static class GameUserSettings
    {
        public static class Effect
        {
            public static SettingsKey<EffectCompilationMode> EffectCompilation = new SettingsKey<EffectCompilationMode>("Package/Game/Effect/EffectCompilation", PackageUserSettings.SettingsContainer, EffectCompilationMode.Local)
            {
                DisplayName = "Effect Compiler"
            };
            public static SettingsKey<bool> RecordUsedEffects = new SettingsKey<bool>("Package/Game/Effect/RecordUsedEffects", PackageUserSettings.SettingsContainer, false)
            {
                DisplayName = "Record used effects"
            };
        }
    }
}

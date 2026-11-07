// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Settings;
using Stride.Core.IO;

namespace Stride.GameStudio.Layout
{
    /// <summary>
    /// The docking layouts of the WPF editor in the <see cref="MRUAdditionalData"/> of a session, which the Avalonia editor keeps
    /// as they are.
    /// </summary>
    internal static class MRUAdditionalDataLayouts
    {
        /// <summary>
        /// The data of the recent sessions, where a new session starts with the default layouts.
        /// </summary>
        public static MRUAdditionalDataCollection CreateCollection()
        {
            return new MRUAdditionalDataCollection(InternalSettings.LoadProfileCopy, GameStudioInternalSettings.MostRecentlyUsedSessionsData, InternalSettings.WriteFile, ResetAllLayouts);
        }

        public static void ResetAllLayouts(this MRUAdditionalDataCollection collection, UFile filePath)
        {
            collection.Update(filePath, ResetAllLayouts);
        }

        public static void ResetLayout(this MRUAdditionalDataCollection collection, UFile filePath)
        {
            collection.Update(filePath, data => data.DockingLayout = GameStudioInternalSettings.DefaultLayout);
        }

        public static void ResetEditorsLayout(this MRUAdditionalDataCollection collection, UFile filePath)
        {
            collection.Update(filePath, data => data.DockingLayoutEditors = GameStudioInternalSettings.DefaultEditorLayout);
        }

        public static void UpdateLayout(this MRUAdditionalDataCollection collection, UFile filePath, string layout)
        {
            collection.Update(filePath, data => data.DockingLayout = layout);
        }

        public static void UpdateEditorsLayout(this MRUAdditionalDataCollection collection, UFile filePath, string layout)
        {
            collection.Update(filePath, data => data.DockingLayoutEditors = layout);
        }

        private static void ResetAllLayouts(MRUAdditionalData data)
        {
            data.DockingLayoutVersion = GameStudioInternalSettings.CurrentLayoutVersion;
            data.DockingLayout = GameStudioInternalSettings.DefaultLayout;
            data.DockingLayoutEditors = GameStudioInternalSettings.DefaultEditorLayout;
        }
    }
}

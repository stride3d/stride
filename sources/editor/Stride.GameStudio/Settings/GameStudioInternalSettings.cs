// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Windows;

using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Settings;

namespace Stride.GameStudio
{
    public static class GameStudioInternalSettings
    {
        public static SettingsContainer SettingsContainer => InternalSettings.SettingsContainer;
        
        public static SettingsKey<List<MRUAdditionalData>> MostRecentlyUsedSessionsData = new SettingsKey<List<MRUAdditionalData>>("Internal/MostRecentlyUsedSessionsData2", SettingsContainer, () => new List<MRUAdditionalData>());
        public static SettingsKey<bool> WindowMaximized = new SettingsKey<bool>("Internal/WindowMaximized", SettingsContainer, false);
        public static SettingsKey<int> WindowWidth = new SettingsKey<int>("Internal/WindowWidth", SettingsContainer, (int)SystemParameters.WorkArea.Width);
        public static SettingsKey<int> WindowHeight = new SettingsKey<int>("Internal/WindowHeight", SettingsContainer, (int)SystemParameters.WorkArea.Height);
        public static SettingsKey<int> WorkAreaWidth = new SettingsKey<int>("Internal/WorkAreaWidth", SettingsContainer, (int)SystemParameters.WorkArea.Width);
        public static SettingsKey<int> WorkAreaHeight = new SettingsKey<int>("Internal/WorkAreaHeight", SettingsContainer, (int)SystemParameters.WorkArea.Height);
        public static SettingsKey<bool> SessionExplorerPanelVisible = new SettingsKey<bool>("Internal/SessionExplorerPanelVisible", SettingsContainer, true);
        public static SettingsKey<bool> AssetViewPanelVisible = new SettingsKey<bool>("Internal/AssetViewPanelVisible", SettingsContainer, true);
        public static SettingsKey<bool> ReferencesPanelVisible = new SettingsKey<bool>("Internal/ReferencesPanelVisible", SettingsContainer, true);
        public static SettingsKey<bool> AssetPreviewPanelVisible = new SettingsKey<bool>("Internal/AssetPreviewPanelVisible", SettingsContainer, true);
        public static SettingsKey<bool> PropertyGridPanelVisible = new SettingsKey<bool>("Internal/PropertyGridPanelVisible", SettingsContainer, true);
        public static SettingsKey<bool> ActionHistoryPanelVisible = new SettingsKey<bool>("Internal/ActionHistoryPanelVisible", SettingsContainer, true);
        public static SettingsKey<bool> AssetLogPanelVisible = new SettingsKey<bool>("Internal/AssetLogPanelVisible", SettingsContainer, true);
        public static SettingsKey<bool> BuildLogPanelVisible = new SettingsKey<bool>("Internal/BuildLogPanelVisible", SettingsContainer, true);

        /// <summary>
        /// Default Game Studio layout when no editors are opened.
        /// </summary>
        internal const string DefaultLayout = "<?xml version=\"1.0\"?><LayoutRoot xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"><RootPanel Orientation=\"Horizontal\"><LayoutPanel Orientation=\"Vertical\"><LayoutPanel Orientation=\"Horizontal\"><LayoutAnchorablePane DockWidth=\"300\"><LayoutAnchorable AutoHideMinWidth=\"300\" AutoHideMinHeight=\"100\" Title=\"Solution explorer\" IsSelected=\"True\" ContentId=\"SolutionExplorer\" LastActivationTimeStamp=\"09/08/2016 20:59:42\" /></LayoutAnchorablePane><LayoutDocumentPane><LayoutAnchorable AutoHideMinWidth=\"300\" AutoHideMinHeight=\"100\" Title=\"Asset view\" IsSelected=\"True\" IsLastFocusedDocument=\"True\" ContentId=\"AssetView\" LastActivationTimeStamp=\"09/08/2016 20:59:41\" /></LayoutDocumentPane></LayoutPanel><LayoutAnchorablePane DockHeight=\"200\"><LayoutAnchorable AutoHideMinWidth=\"100\" AutoHideMinHeight=\"200\" Title=\"References\" ContentId=\"References\" /><LayoutAnchorable AutoHideMinWidth=\"100\" AutoHideMinHeight=\"200\" IsSelected=\"True\" ContentId=\"AssetLog\"/><LayoutAnchorable AutoHideMinWidth=\"100\" AutoHideMinHeight=\"200\" ContentId=\"BuildLog\"/></LayoutAnchorablePane></LayoutPanel><LayoutAnchorablePaneGroup Orientation=\"Vertical\" DockWidth=\"400\"><LayoutAnchorablePane DockHeight=\"2*\"><LayoutAnchorable AutoHideMinWidth=\"400\" AutoHideMinHeight=\"100\" Title=\"Property grid\" IsSelected=\"True\" ContentId=\"PropertyGrid\" /></LayoutAnchorablePane><LayoutAnchorablePaneGroup Orientation=\"Horizontal\"><LayoutAnchorablePane><LayoutAnchorable AutoHideMinWidth=\"400\" AutoHideMinHeight=\"100\" Title=\"Asset preview\" IsSelected=\"True\" ContentId=\"AssetPreview\" /><LayoutAnchorable AutoHideMinWidth=\"400\" AutoHideMinHeight=\"100\" Title=\"Action history\" ContentId=\"ActionHistory\" /></LayoutAnchorablePane></LayoutAnchorablePaneGroup></LayoutAnchorablePaneGroup></RootPanel><TopSide /><RightSide /><LeftSide /><BottomSide /><FloatingWindows /><Hidden /></LayoutRoot>";
        /// <summary>
        /// Default Game Studio layout with editors opened.
        /// </summary>
        internal const string DefaultEditorLayout = "<?xml version=\"1.0\"?><LayoutRoot xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"><RootPanel Orientation=\"Horizontal\"><LayoutPanel Orientation=\"Vertical\"><LayoutDocumentPane /><LayoutAnchorablePaneGroup Orientation=\"Horizontal\" DockHeight=\"300\"><LayoutAnchorablePane DockWidth=\"1*\"><LayoutAnchorable AutoHideMinHeight=\"300\" Title=\"Solution explorer\" ContentId=\"SolutionExplorer\" /></LayoutAnchorablePane><LayoutAnchorablePane DockWidth=\"3*\"><LayoutAnchorable AutoHideMinHeight=\"300\" Title=\"Asset view\" IsSelected=\"True\" ContentId=\"AssetView\"/><LayoutAnchorable AutoHideMinHeight=\"300\" ContentId=\"AssetLog\" /><LayoutAnchorable AutoHideMinHeight=\"300\" ContentId=\"BuildLog\" /></LayoutAnchorablePane></LayoutAnchorablePaneGroup></LayoutPanel><LayoutAnchorablePaneGroup Orientation=\"Vertical\" DockWidth=\"400\"><LayoutAnchorablePane DockHeight=\"4*\"><LayoutAnchorable AutoHideMinWidth=\"400\" Title=\"Property grid\" IsSelected=\"True\" ContentId=\"PropertyGrid\" /></LayoutAnchorablePane><LayoutAnchorablePaneGroup Orientation=\"Horizontal\" DockHeight=\"2*\"><LayoutAnchorablePane><LayoutAnchorable AutoHideMinWidth=\"400\" Title=\"Asset preview\" ContentId=\"AssetPreview\" /><LayoutAnchorable AutoHideMinWidth=\"400\" Title=\"Action history\" ContentId=\"ActionHistory\" /><LayoutAnchorable AutoHideMinWidth=\"400\" Title=\"References\" ContentId=\"References\" IsSelected=\"True\" /></LayoutAnchorablePane></LayoutAnchorablePaneGroup></LayoutAnchorablePaneGroup></RootPanel><TopSide /><RightSide /><LeftSide /><BottomSide /><FloatingWindows /><Hidden /></LayoutRoot>";

        /// <summary>
        /// Current version of the layout. If saved version is lower, layouts will be reset to default values.
        /// </summary>
        /// <remarks>Bump when making changes to layout and want to force user to reset to default version.</remarks>
        internal const int CurrentLayoutVersion = 1;
    }
}

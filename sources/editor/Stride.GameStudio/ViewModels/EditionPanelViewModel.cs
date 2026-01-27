// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Runtime.CompilerServices;
using Stride.Core.Settings;
using Stride.Core.Presentation.ViewModels;

namespace Stride.GameStudio.ViewModels
{
    /// <summary>
    /// This view model represents the state of the different panel of the editor window.
    /// </summary>
    public class EditionPanelViewModel : DispatcherViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EditionPanelViewModel"/> class.
        /// </summary>
        /// <param name="serviceProvider"></param>
        public EditionPanelViewModel(IViewModelServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        /// <summary>
        /// Gets or sets whether the session explorer panel is visible.
        /// </summary>
        public bool SessionExplorerPanelVisible { get; set { SetValue(ref field, value, GameStudioInternalSettings.SessionExplorerPanelVisible); } } = true;

        /// <summary>
        /// Gets or sets whether the asset view panel is visible.
        /// </summary>
        public bool AssetViewPanelVisible { get; set { SetValue(ref field, value, GameStudioInternalSettings.AssetViewPanelVisible); } } = true;

        /// <summary>
        /// Gets or sets whether the references panel is visible.
        /// </summary>
        public bool ReferencesPanelVisible { get; set { SetValue(ref field, value, GameStudioInternalSettings.ReferencesPanelVisible); } } = true;

        /// <summary>
        /// Gets or sets whether the preview panel is visible.
        /// </summary>
        public bool AssetPreviewPanelVisible { get; set { SetValue(ref field, value, GameStudioInternalSettings.AssetPreviewPanelVisible); } } = true;

        /// <summary>
        /// Gets or sets whether the property grid panel is visible.
        /// </summary>
        public bool PropertyGridPanelVisible { get; set { SetValue(ref field, value, GameStudioInternalSettings.PropertyGridPanelVisible); } } = true;

        /// <summary>
        /// Gets or sets whether the action history panel is visible.
        /// </summary>
        public bool ActionHistoryPanelVisible { get; set { SetValue(ref field, value, GameStudioInternalSettings.ActionHistoryPanelVisible); } } = true;

        /// <summary>
        /// Gets or sets whether the asset log panel is visible.
        /// </summary>
        public bool AssetLogPanelVisible { get; set { SetValue(ref field, value, GameStudioInternalSettings.AssetLogPanelVisible); } } = true;

        /// <summary>
        /// Gets or sets whether the build log panel is visible.
        /// </summary>
        public bool BuildLogPanelVisible { get; set { SetValue(ref field, value, GameStudioInternalSettings.BuildLogPanelVisible); } } = true;

        /// <summary>
        /// Loads the visible/hidden status of each panel from the settings.
        /// </summary>
        public void LoadFromSettings()
        {
            SessionExplorerPanelVisible = GameStudioInternalSettings.SessionExplorerPanelVisible.GetValue();
            AssetViewPanelVisible = GameStudioInternalSettings.AssetViewPanelVisible.GetValue();
            ReferencesPanelVisible = GameStudioInternalSettings.ReferencesPanelVisible.GetValue();
            AssetPreviewPanelVisible = GameStudioInternalSettings.AssetPreviewPanelVisible.GetValue();
            PropertyGridPanelVisible = GameStudioInternalSettings.PropertyGridPanelVisible.GetValue();
            ActionHistoryPanelVisible = GameStudioInternalSettings.ActionHistoryPanelVisible.GetValue();
            AssetLogPanelVisible = GameStudioInternalSettings.AssetLogPanelVisible.GetValue();
            BuildLogPanelVisible = GameStudioInternalSettings.BuildLogPanelVisible.GetValue();
        }

        /// <summary>
        /// Sets all panels to be visible.
        /// </summary>
        public void SetAllPanelVisible()
        {
            SessionExplorerPanelVisible = true;
            AssetViewPanelVisible = true;
            ReferencesPanelVisible = true;
            AssetPreviewPanelVisible = true;
            PropertyGridPanelVisible = true;
            ActionHistoryPanelVisible = true;
            AssetLogPanelVisible = true;
            BuildLogPanelVisible = true;
        }

        private void SetValue(ref bool field, bool value, SettingsKey<bool> settingsKey, [CallerMemberName] string propertyName = null)
        {
            SetValue(ref field, value, propertyName);
            settingsKey.SetValue(value);
        }
    }
}

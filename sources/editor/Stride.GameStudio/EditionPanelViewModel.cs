// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Runtime.CompilerServices;
using Stride.Core.Settings;
using Stride.Core.Presentation.ViewModel;

namespace Stride.GameStudio
{
    /// <summary>
    /// This view model represents the state of the different panel of the editor window.
    /// </summary>
    public class EditionPanelViewModel : DispatcherViewModel
    {
        private bool sessionExplorerPanelVisible = true;
        private bool assetViewPanelVisible = true;
        private bool referencesPanelVisible = true;
        private bool assetPreviewPanelVisible = true;
        private bool propertyGridPanelVisible = true;
        private bool actionHistoryPanelVisible = true;
        private bool assetLogPanelVisible = true;
        private bool buildLogPanelVisible = true;

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
        public bool SessionExplorerPanelVisible { get { return sessionExplorerPanelVisible; } set { SetValue(ref sessionExplorerPanelVisible, value, GameStudioInternalSettings.SessionExplorerPanelVisible); } }

        /// <summary>
        /// Gets or sets whether the asset view panel is visible.
        /// </summary>
        public bool AssetViewPanelVisible { get { return assetViewPanelVisible; } set { SetValue(ref assetViewPanelVisible, value, GameStudioInternalSettings.AssetViewPanelVisible); } }

        /// <summary>
        /// Gets or sets whether the references panel is visible.
        /// </summary>
        public bool ReferencesPanelVisible { get { return referencesPanelVisible; } set { SetValue(ref referencesPanelVisible, value, GameStudioInternalSettings.ReferencesPanelVisible); } }

        /// <summary>
        /// Gets or sets whether the preview panel is visible.
        /// </summary>
        public bool AssetPreviewPanelVisible { get { return assetPreviewPanelVisible; } set { SetValue(ref assetPreviewPanelVisible, value, GameStudioInternalSettings.AssetPreviewPanelVisible); } }

        /// <summary>
        /// Gets or sets whether the property grid panel is visible.
        /// </summary>
        public bool PropertyGridPanelVisible { get { return propertyGridPanelVisible; } set { SetValue(ref propertyGridPanelVisible, value, GameStudioInternalSettings.PropertyGridPanelVisible); } }

        /// <summary>
        /// Gets or sets whether the action history panel is visible.
        /// </summary>
        public bool ActionHistoryPanelVisible { get { return actionHistoryPanelVisible; } set { SetValue(ref actionHistoryPanelVisible, value, GameStudioInternalSettings.ActionHistoryPanelVisible); } }

        /// <summary>
        /// Gets or sets whether the asset log panel is visible.
        /// </summary>
        public bool AssetLogPanelVisible { get { return assetLogPanelVisible; } set { SetValue(ref assetLogPanelVisible, value, GameStudioInternalSettings.AssetLogPanelVisible); } }

        /// <summary>
        /// Gets or sets whether the build log panel is visible.
        /// </summary>
        public bool BuildLogPanelVisible { get { return buildLogPanelVisible; } set { SetValue(ref buildLogPanelVisible, value, GameStudioInternalSettings.BuildLogPanelVisible); } }

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

        private void SetValue(ref bool field, bool value, SettingsKey<bool> settingsKey, [CallerMemberName]string propertyName = null)
        {
            SetValue(ref field, value, propertyName);
            settingsKey.SetValue(value);
        }
    }
}

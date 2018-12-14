// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Editor.Settings;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Extensions;
using Xenko.Core.Presentation.Behaviors;
using Xenko.Core.Presentation.Extensions;
using Xceed.Wpf.AvalonDock;
using Xceed.Wpf.AvalonDock.Layout;
using Xceed.Wpf.AvalonDock.Layout.Serialization;

namespace Xenko.GameStudio
{
    /// <summary>
    /// A class that manages the docking layout of a <see cref="GameStudioWindow"/>, including switching between Editor and Normal mode, saving/loading
    /// layout information into the settings file, and resetting layout to default.
    /// </summary>
    internal class DockingLayoutManager
    {
        private readonly MRUAdditionalDataCollection mruDataCollection = new MRUAdditionalDataCollection(InternalSettings.LoadProfileCopy, GameStudioInternalSettings.MostRecentlyUsedSessionsData, InternalSettings.WriteFile);
        private readonly GameStudioWindow gameStudioWindow;
        private readonly SessionViewModel session;
        private bool isInEditorLayout;

        private struct BindingInfo
        {
            public readonly Binding Binding;
            public readonly DependencyProperty Property;

            private BindingInfo(Binding binding, DependencyProperty property)
            {
                Binding = binding;
                Property = property;
            }

            public static BindingInfo FromBindingExpression(BindingExpression bindingExpression)
            {
                return new BindingInfo(bindingExpression.ParentBinding.CloneBinding(), bindingExpression.TargetProperty);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DockingLayoutManager"/> class.
        /// </summary>
        /// <param name="gameStudioWindow">The Game Studio window that contains the docking layout.</param>
        /// <param name="session">The session opened in the Game Studio.</param>
        public DockingLayoutManager(GameStudioWindow gameStudioWindow, SessionViewModel session)
        {
            if (gameStudioWindow == null) throw new ArgumentNullException(nameof(gameStudioWindow));
            if (session == null) throw new ArgumentNullException(nameof(session));
            this.gameStudioWindow = gameStudioWindow;
            this.session = session;
        }

        /// <summary>
        /// Gets the <see cref="DockingManager"/> handled by this instance.
        /// </summary>
        public DockingManager DockingManager => gameStudioWindow.Docking;

        /// <summary>
        /// Saves the currently active layout (Editor or Normal) into the settings file.
        /// </summary>
        public void SaveCurrentLayout()
        {
            var layout = GetDockingLayout();
            if (isInEditorLayout)
                mruDataCollection.UpdateEditorsLayout(session.SessionFilePath, layout);
            else
                mruDataCollection.UpdateLayout(session.SessionFilePath, layout);
        }

        /// <summary>
        /// Saves the given list of assets as currently open in the settings file.
        /// </summary>
        /// <param name="openAssets">The list of asset to save as open.</param>
        /// <remarks>Assets that are currently deleted or that are not editable will be filtered out.</remarks>
        public void SaveOpenAssets(IEnumerable<AssetViewModel> openAssets)
        {
            // Filter assets
            var assetIds = openAssets?.Where(a => !a.IsDeleted && a.IsEditable).Select(a => a.AssetItem.Id) ?? Enumerable.Empty<AssetId>();
            mruDataCollection.UpdateOpenedAssets(session.SessionFilePath, assetIds);
        }

        /// <summary>
        /// Loads the list of open assets that are currently saved in the settings file.
        /// </summary>
        /// <returns>A collection of <see cref="AssetId"/> representing asset that were saved as open.</returns>
        public IEnumerable<AssetId> LoadOpenAssets()
        {
            var mruData = mruDataCollection.GetData(session.SessionFilePath);
            return mruData != null ? mruData.OpenedAssets : Enumerable.Empty<AssetId>();
        }

        /// <summary>
        /// Switches to the Editor layout, if it's not already the currently active layout.
        /// </summary>
        /// <remarks>This will save the previous layout. If the new layout cannot be loaded from settings, it will be reset to default before being applied</remarks>
        public void SwitchToEditorLayout() => SwitchToLayout(true);

        /// <summary>
        /// Switches to the Normal layout, if it's not already the currently active layout.
        /// </summary>
        /// <remarks>This will save the previous layout. If the new layout cannot be loaded from settings, it will be reset to default before being applied</remarks>
        public void SwitchToNormalLayout() => SwitchToLayout(false);

        /// <summary>
        /// Reloads the currently active layout from settings file (or default if the layout cannot be loaded from settings).
        /// </summary>
        /// <remarks>The current state of the active layout will be lost after calling this method.</remarks>
        public void ReloadCurrentLayout()
        {
            LoadLayoutFromSettings(mruDataCollection.GetData(session.SessionFilePath));
        }

        /// <summary>
        /// Resets all layouts to factory settings and reload the current layout.
        /// </summary>
        /// <remarks>This does not affect currently open assets and does not change the currently active layout.</remarks>
        public void ResetAllLayouts()
        {
            mruDataCollection.ResetAllLayouts(session.SessionFilePath);
            GameStudioViewModel.GameStudio.Panels.SetAllPanelVisible();
            ReloadCurrentLayout();
        }

        private void SwitchToLayout(bool toEditorLayout)
        {
            if (isInEditorLayout == toEditorLayout)
                return;

            SaveCurrentLayout();
            isInEditorLayout = toEditorLayout;
            ReloadCurrentLayout();
        }

        private void LoadLayoutFromSettings(MRUAdditionalData data, bool resetLayoutIfFailed = true)
        {
            if (data == null)
                return;

            var layout = isInEditorLayout ? data.DockingLayoutEditors : data.DockingLayout;
            BindableSelectedItemsControl.DisableBindings = true;
            try
            {
                // This exception is normal and will trigger a reset of the layout, since no layout can be loaded from the settings file.
                if (GameStudioInternalSettings.CurrentLayoutVersion != data.DockingLayoutVersion)
                    throw new InvalidOperationException("Layout is out of date, need reset.");

                // This exception is normal and will trigger a reset of the layout, since no layout can be loaded from the settings file.
                if (string.IsNullOrWhiteSpace(layout))
                    throw new InvalidOperationException("No layout available in the settings file.");

                ApplyDockingLayout(layout);
            }
            catch (Exception)
            {
                if (!resetLayoutIfFailed)
                    return;

                // Erase saved layout if we're unable to load it.
                if (GameStudioInternalSettings.CurrentLayoutVersion != data.DockingLayoutVersion)
                {
                    mruDataCollection.ResetAllLayouts(session.SessionFilePath);
                }
                else if (isInEditorLayout)
                {
                    mruDataCollection.ResetEditorsLayout(session.SessionFilePath);
                }
                else
                {
                    mruDataCollection.ResetLayout(session.SessionFilePath);
                }
                // And attempt to load the reset layout.
                LoadLayoutFromSettings(mruDataCollection.GetData(session.SessionFilePath), false);
            }
            finally
            {
                BindableSelectedItemsControl.DisableBindings = false;
            }
        }

        private void ApplyDockingLayout(string text)
        {
            // Save the binding expressions of all the current anchorables
            var bindings = new Dictionary<string, List<BindingInfo>>();
            foreach (var anchorable in AvalonDockHelper.GetAllAnchorables(DockingManager).Where(x => !string.IsNullOrEmpty(x.ContentId)))
            {
                var titleBindingInfo = BindingInfo.FromBindingExpression(BindingOperations.GetBindingExpression(anchorable, LayoutContent.TitleProperty));
                var isVisibleBindingInfo = BindingInfo.FromBindingExpression(BindingOperations.GetBindingExpression(anchorable, AvalonDockHelper.IsVisibleProperty));
                bindings.Add(anchorable.ContentId, new List<BindingInfo> { titleBindingInfo, isVisibleBindingInfo });
            }
            // Unregister docking manager
            AvalonDockHelper.UnregisterDockingManager(DockingManager);
            // Deserialize the string
            using (var stream = new MemoryStream())
            {
                var writer = new StreamWriter(stream);
                writer.Write(text);
                writer.Flush();
                stream.Seek(0, SeekOrigin.Begin);
                var serializer = new XmlLayoutSerializer(DockingManager);
                serializer.Deserialize(stream);
            }
            // Apply saved the binding expressions to the newly deserialized anchorables
            foreach (var anchorable in AvalonDockHelper.GetAllAnchorables(DockingManager).Where(x => !string.IsNullOrEmpty(x.ContentId)))
            {
                List<BindingInfo> bindingInfos;
                if (bindings.TryGetValue(anchorable.ContentId, out bindingInfos))
                {
                    foreach (var bindingInfo in bindingInfos)
                    {
                        BindingOperations.SetBinding(anchorable, bindingInfo.Property, bindingInfo.Binding);
                    }
                }
            }
            // Re-register docking manager with new layout
            AvalonDockHelper.RegisterDockingManager(session.ServiceProvider, DockingManager);
        }

        private string GetDockingLayout()
        {
            try
            {
                using (var stream = new MemoryStream())
                {
                    var serializer = new XmlLayoutSerializer(DockingManager);
                    serializer.Serialize(stream);

                    stream.Seek(0, SeekOrigin.Begin);
                    var reader = new StreamReader(stream);
                    var text = reader.ReadToEnd();
                    return text;
                }
            }
            catch (Exception)
            {
                // If an error occurs, we do not want to save a half-written XML file, so let's reset the layout instead.
                return string.Empty;
            }
        }
    }
}

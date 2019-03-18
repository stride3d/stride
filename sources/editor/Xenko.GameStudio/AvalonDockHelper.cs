// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Xenko.Core.Extensions;
using Xenko.Core.Presentation.Commands;
using Xenko.Core.Presentation.ViewModel;
using Xceed.Wpf.AvalonDock;
using Xceed.Wpf.AvalonDock.Controls;
using Xceed.Wpf.AvalonDock.Layout;

namespace Xenko.GameStudio
{
    public static class AvalonDockHelper
    {
        private static readonly List<LayoutAnchorable> VisiblityChangingAnchorable = new List<LayoutAnchorable>();
        private static IViewModelServiceProvider serviceProvider;
        public static readonly DependencyProperty IsVisibleProperty = DependencyProperty.RegisterAttached("IsVisible", typeof(bool), typeof(AvalonDockHelper), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, IsVisibleChanged));

        public static bool GetIsVisible(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsVisibleProperty);
        }

        public static void SetIsVisible(DependencyObject obj, bool value)
        {
            obj.SetValue(IsVisibleProperty, value);
        }

        public static void RegisterDockingManager(IViewModelServiceProvider viewModelServiceProvider, DockingManager docking)
        {
            serviceProvider = viewModelServiceProvider;
            foreach (var anchorable in GetAllAnchorables(docking))
            {
                if (!string.IsNullOrEmpty(anchorable.ContentId))
                    anchorable.IsVisibleChanged += AnchorableIsVisibleChanged;
                AdjustAnchorableHideAndCloseCommands(anchorable);
            }

            var layoutRoot = (LayoutRoot)docking.LayoutRootPanel.Model.Root;
            layoutRoot.ElementAdded += ElementAdded;
        }

        public static void UnregisterDockingManager(DockingManager docking)
        {
            serviceProvider = null;
            var layoutRoot = (LayoutRoot)docking.LayoutRootPanel?.Model.Root;
            if (layoutRoot != null)
            {
                layoutRoot.ElementAdded -= ElementAdded;
            }

            foreach (var anchorable in GetAllAnchorables(docking))
            {
                anchorable.SetValue(IsVisibleProperty, DependencyProperty.UnsetValue);
                anchorable.IsVisibleChanged -= AnchorableIsVisibleChanged;
            }
        }

        public static IEnumerable<LayoutAnchorable> GetAllAnchorables(DockingManager docking)
        {
            return GetAllLayout<LayoutAnchorable>(docking);
        }

        public static LayoutDocumentPane GetDocumentPane(DockingManager docking)
        {
            return GetAllLayout<LayoutDocumentPane>(docking).First();
        }

        public static IReadOnlyCollection<T> GetAllLayout<T>(DockingManager docking)
        {
            var roots = docking.Layout.ToEnumerable<ILayoutElement>().Concat(docking.FloatingWindows.Select(x => x.Model)).Concat(docking.Layout.Hidden);
            return new HashSet<T>(roots.SelectMany(x => x.Descendents()).OfType<T>());
        }

        private static void IsVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var content = d as LayoutAnchorable;
            if (content != null)
            {
                // This means that the anchorable is not loaded yet
                if (!content.IsVisible && !content.IsHidden)
                    return;

                VisiblityChangingAnchorable.Add(content);
                content.IsVisible = (bool)e.NewValue;
                VisiblityChangingAnchorable.Remove(content);
            }
        }

        private static void AnchorableIsVisibleChanged(object sender, EventArgs e)
        {
            var content = (LayoutAnchorable)sender;
            if (!VisiblityChangingAnchorable.Contains(content))
            {
                content.SetCurrentValue(IsVisibleProperty, content.IsVisible);
            }
        }

        private static void ElementAdded(object sender, LayoutElementEventArgs e)
        {
            if (e.Element is LayoutAnchorable anchorable)
            {
                AdjustAnchorableHideAndCloseCommands(anchorable);
            }
            else
            {
                foreach (var anchorable2 in e.Element.Descendents().OfType<LayoutAnchorable>())
                {
                    AdjustAnchorableHideAndCloseCommands(anchorable2);
                }
            }
        }

        /// <summary>
        /// Adjusts the behavior of commands to close or hide anchorable.
        /// </summary>
        /// <param name="anchorable">The anchorable to adjust.</param>
        /// <remarks>
        /// In AvalonDock, the choice between closing and hiding depends on whether the anchorable is in a document pane or an anchorable pane. However,
        /// this condition is irrelevant to the choice of closing or hiding the anchorable. This method allows to adjust the behavior of the close/hide
        /// commands to have a single behavior per anchorable.
        /// An anchorable is considered to be persistent (always hide) if it has a non-null 
        /// Calling this method is mandatory on every anchorable created for the game studio.
        /// </remarks>
        private static void AdjustAnchorableHideAndCloseCommands(LayoutAnchorable anchorable)
        {
            var layoutItem = (LayoutAnchorableItem)anchorable.Root.Manager.GetLayoutItemFromModel(anchorable);
            if (layoutItem == null)
                throw new InvalidOperationException("The anchorable must be added to the docking manager before calling this method.");

            // There's a bug in AvalonDock 3.4.0 which sets CanClose to false once a LayoutAnchorable is dragged into a new floating window or a new pane.
            // This is because ResetCanCloseInternal() is called without SetCanCloseInternal() so value gets reset to false.
            layoutItem.CanClose = true;

            var isPersistent = !string.IsNullOrEmpty(anchorable.ContentId);
            if (isPersistent)
                layoutItem.CloseCommand = new AnonymousCommand(serviceProvider, () => layoutItem.HideCommand.Execute(null), () => layoutItem.HideCommand.CanExecute(null));
            else
                layoutItem.HideCommand = new AnonymousCommand(serviceProvider, () => layoutItem.CloseCommand.Execute(null), () => layoutItem.CloseCommand.CanExecute(null));
        }
    }
}

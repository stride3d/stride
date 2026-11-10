// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Annotations;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Views
{
    /// <summary>
    /// The WPF entity picker: a view of the shared <see cref="EntityPickerViewModel"/>, as the Avalonia picker window.
    /// </summary>
    public sealed partial class EntityPickerWindow
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EntityPickerWindow"/> class.
        /// </summary>
        /// <param name="editor">The editor whose hierarchy is shown.</param>
        /// <param name="componentType">The type of the component to pick, or <c>null</c> to pick an entity.</param>
        /// <param name="filter">The items of the hierarchy that are shown, with their children, or <c>null</c> for all of them.</param>
        public EntityPickerWindow([NotNull] EntityHierarchyEditorViewModel editor, Type componentType, Func<EntityHierarchyItemViewModel, bool> filter)
        {
            ArgumentNullException.ThrowIfNull(editor);

            InitializeComponent();
            Picker = new EntityPickerViewModel(editor, componentType, filter);
            // Wider with the list of the components
            if (Picker.ComponentType != null)
                Width *= 2;
            DataContext = Picker;
        }

        /// <summary>
        /// Gets the view model of the picker.
        /// </summary>
        public EntityPickerViewModel Picker { get; }
    }
}

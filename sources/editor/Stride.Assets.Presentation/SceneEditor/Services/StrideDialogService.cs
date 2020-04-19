// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Presentation.Services;
using Stride.Engine;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Views;

namespace Stride.Assets.Presentation.SceneEditor.Services
{
    /// <summary>
    /// This class is the default implementation of the <see cref="IStrideDialogService"/>.
    /// </summary>
    internal class StrideDialogService : IStrideDialogService
    {
        /// <inheritdoc/>
        public IEntityPickerDialog CreateEntityPickerDialog(EntityHierarchyEditorViewModel editor)
        {
            if (editor == null) throw new ArgumentNullException(nameof(editor));
            var picker = new EntityPickerWindow(editor, null);
            return picker;
        }

        /// <inheritdoc/>
        public IEntityPickerDialog CreateEntityComponentPickerDialog(EntityHierarchyEditorViewModel editor, Type componentType)
        {
            if (editor == null) throw new ArgumentNullException(nameof(editor));
            if (!typeof(EntityComponent).IsAssignableFrom(componentType))
                throw new ArgumentException(@"The given component type does not inherit from EntityComponent.", nameof(componentType));

            var picker = new EntityPickerWindow(editor, componentType);
            return picker;
        }
    }
}

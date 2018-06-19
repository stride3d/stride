// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Presentation.Services;
using Xenko.Engine;
using Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Views;

namespace Xenko.Assets.Presentation.SceneEditor.Services
{
    /// <summary>
    /// This class is the default implementation of the <see cref="IXenkoDialogService"/>.
    /// </summary>
    internal class XenkoDialogService : IXenkoDialogService
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

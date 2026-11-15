// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;
using Stride.Assets.Editor.Services;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Views;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.Services;

namespace Stride.Assets.Presentation.SceneEditor.Services
{
    /// <summary>
    /// The WPF <see cref="IEntityPickerService"/>: shows <see cref="EntityPickerWindow"/>, a view of the shared
    /// <see cref="EntityPickerViewModel"/>, modally.
    /// </summary>
    internal sealed class WpfEntityPickerService : IEntityPickerService
    {
        /// <inheritdoc/>
        public async Task<IPickedEntity> PickEntityAsync(EntityHierarchyEditorViewModel editor, Type componentType, Func<EntityHierarchyItemViewModel, bool> filter)
        {
            if (editor == null) throw new ArgumentNullException(nameof(editor));

            var picker = new EntityPickerWindow(editor, componentType, filter);
            var result = await picker.ShowModal();
            return result == DialogResult.Ok ? picker.Picker.PickedEntity : null;
        }
    }
}

// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Avalonia.Templates;
using Stride.Assets.Editor.Services;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Presentation.ViewModels;

namespace Stride.Assets.Editor.Avalonia.Views;

/// <summary>
/// The Avalonia <see cref="IEntityPickerService"/>: shows <see cref="EntityPickerWindow"/> modally over the active window.
/// </summary>
internal sealed class AvaloniaEntityPickerService : IEntityPickerService
{
    public async Task<IPickedEntity?> PickEntityAsync(EntityHierarchyEditorViewModel editor, Type? componentType, Func<EntityHierarchyItemViewModel, bool> filter)
    {
        ArgumentNullException.ThrowIfNull(editor);
        // The view model reads the hierarchy, which the UI thread owns
        var picker = await editor.Dispatcher.InvokeAsync(() => new EntityPickerViewModel(editor, componentType, filter));
        var window = new EntityPickerWindow { DataContext = picker };
        // Wider with the list of the components
        if (picker.ComponentType is not null)
            window.Width *= 1.6;
        await TemplateDialogs.ShowModalAsync(window);
        return window.Picked ? picker.PickedEntity : null;
    }
}

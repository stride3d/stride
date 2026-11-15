// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Avalonia.Commands;
using Stride.Core.Presentation.Avalonia.Controls;

namespace Stride.GameStudio.Avalonia.Views;

public partial class PropertyGridView : UserControl
{
    public PropertyGridView()
    {
        InitializeComponent();
        // The event is declared as a non-generic RoutedEvent, which the XAML compiler cannot wire to a typed handler
        Properties.PrepareItem += OnPrepareItem;
    }

    /// <summary>
    /// Enter in the "Add new tag" box adds the tag to the selected assets and clears the box.
    /// </summary>
    private void OnNewTagKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || sender is not TextBox textBox)
            return;

        if (DataContext is SessionObjectPropertiesViewModel { Session: SessionViewModel { AssetTags.AddTagCommand: { } addTag } } && addTag.CanExecute(textBox.Text))
        {
            addTag.Execute(textBox.Text);
        }
        textBox.Clear();
        e.Handled = true;
    }

    /// <summary>
    /// Copy and Paste from the Edit menu, or their shortcuts, work on the property that has the focus: each property gets the
    /// bindings of its own commands, which follow the property it shows.
    /// </summary>
    private void OnPrepareItem(object? sender, PropertyViewItemEventArgs e)
    {
        var bindings = EditCommands.GetBindings(e.Container);
        if (bindings.Count > 0)
            return;

        bindings.Add(NodeCommandBinding(e.Container, EditCommandKind.Copy, CopyPropertyCommand.CommandName));
        bindings.Add(NodeCommandBinding(e.Container, EditCommandKind.Paste, PastePropertyCommand.CommandName));
        return;

        static EditCommandBinding NodeCommandBinding(PropertyViewItem item, EditCommandKind kind, string commandName)
        {
            var binding = new EditCommandBinding { Kind = kind };
            binding.Bind(EditCommandBinding.CommandProperty, new Binding($"{nameof(DataContext)}[{commandName}]") { Source = item });
            return binding;
        }
    }
}

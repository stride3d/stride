// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.VisualTree;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Presentation.Avalonia.Commands;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.Core.Presentation.Quantum.ViewModels;

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
    /// The shortcut of Replace, which pastes the copied value in place of the property (WPF binds it on its property grid).
    /// </summary>
    public static KeyGesture ReplaceGesture { get; } = new(Key.V, KeyModifiers.Control | KeyModifiers.Shift);

    /// <summary>
    /// Replace works on the property that has the focus, or holds the focused editor.
    /// </summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled || !ReplaceGesture.Matches(e))
            return;

        var item = (e.Source as Visual)?.FindAncestorOfType<PropertyViewItem>(includeSelf: true);
        if (item?.DataContext is NodeViewModel node && node.GetCommand(ReplacePropertyCommand.CommandName) is { } replace && replace.CanExecute(null))
        {
            replace.Execute(null);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Enter in the "Add new tag" box adds the tag to the selected assets and clears the box, as the WPF box does on validation.
    /// </summary>
    private void OnNewTagKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || sender is not TextBox textBox)
            return;

        if (DataContext is SessionObjectPropertiesViewModel { Session.AssetTags.AddTagCommand: { } addTag } && addTag.CanExecute(textBox.Text))
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

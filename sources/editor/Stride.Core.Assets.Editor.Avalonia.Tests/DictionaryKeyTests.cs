// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Stride.Core.Assets.Editor.Avalonia.Tests.Helpers;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Updaters;
using Stride.Core.Presentation.Avalonia.Controls;

namespace Stride.Core.Assets.Editor.Avalonia.Tests;

/// <summary>
/// The button of a dictionary property asks for the key of the new entry in a box, which Enter adds.
/// </summary>
public sealed class DictionaryKeyTests
{
    [AvaloniaFact]
    public void EnterAddsAnEntryWithTheTypedTextKeyAndClosesTheBox()
    {
        var sample = new KeyedSample();
        var (window, item, box) = OpenKeyBox(sample, nameof(KeyedSample.Names));
        try
        {
            Assert.Equal("New key", box.Text);
            Assert.True(box.IsFocused);
            window.KeyTextInput("lamp");
            window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            PropertyViewWindow.Refresh(window);

            Assert.Equal(["a", "lamp"], sample.Names.Keys.Order());
            Assert.False(KeyBoxOpener(item).IsChecked);

            // The box shows its text again the next time, and the same key adds nothing
            KeyBoxOpener(item).IsChecked = true;
            PropertyViewWindow.Refresh(window);
            Assert.Equal("New key", box.Text);
            window.KeyTextInput("lamp");
            window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            Assert.Equal(2, sample.Names.Count);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void EnterAddsAnEntryWithTheTypedNumberKeyAndEscapeAddsNothing()
    {
        var sample = new KeyedSample();
        var (window, item, _) = OpenKeyBox(sample, nameof(KeyedSample.Numbers));
        try
        {
            window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
            PropertyViewWindow.Refresh(window);
            Assert.False(KeyBoxOpener(item).IsChecked);
            Assert.Equal([1], sample.Numbers.Keys);

            KeyBoxOpener(item).IsChecked = true;
            PropertyViewWindow.Refresh(window);
            window.KeyTextInput("7");
            window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);

            Assert.Equal([1, 7], sample.Numbers.Keys.Order());
        }
        finally
        {
            window.Close();
        }
    }

    private static (Window Window, PropertyViewItem Item, TextBox Box) OpenKeyBox(KeyedSample sample, string property)
    {
        // The updater gives a dictionary the type of its keys, which picks the template that asks for a key
        var context = new NodeViewModelContext(new DictionaryNodeUpdater());
        context.Commands.Add(new AddPrimitiveKeyCommand());
        var (window, propertyView) = PropertyViewWindow.Show(sample, context);
        var item = propertyView.Item(property);
        KeyBoxOpener(item).IsChecked = true;
        PropertyViewWindow.Refresh(window);
        var popup = item.GetVisualDescendants().OfType<Popup>().Single(x => x.Name == "PART_Popup");
        Assert.True(popup.IsOpen);
        var box = Assert.IsType<TextBox>(popup.Child!.GetVisualDescendants().OfType<TextBox>().Single());
        Dispatcher.UIThread.RunJobs();
        return (window, item, box);
    }

    private static ToggleButton KeyBoxOpener(PropertyViewItem item)
    {
        return item.GetVisualDescendants().OfType<ToggleButton>().Single(x => x.Name == "ToggleButton");
    }

    public sealed class KeyedSample
    {
        public Dictionary<string, int> Names { get; set; } = new() { ["a"] = 1 };

        public Dictionary<int, int> Numbers { get; set; } = new() { [1] = 1 };
    }
}

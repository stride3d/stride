// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Avalonia.Controls;

namespace Stride.Core.Presentation.Avalonia.Tests.Controls;

/// <summary>
/// The color editors show their value as hexadecimal text, one byte per channel. Showing a value that has more precision
/// than that must not write the rounded text back to the source; a color the user types or picks is written.
/// </summary>
public sealed class ColorEditorTests
{
    public static TheoryData<Type, object> EditorsAndValues => new()
    {
        // Not exact on a byte per channel: 0.5 shows as 7F (0.498), 0.25 as 3F (0.247)
        { typeof(Color3Editor), new Color3(1f, 0.5f, 0.25f) },
        { typeof(Color4Editor), new Color4(1f, 0.5f, 0.25f, 0.75f) },
        { typeof(ColorEditor), new Color(255, 128, 64, 192) },
    };

    [AvaloniaTheory]
    [MemberData(nameof(EditorsAndValues))]
    public void ShowingAValueWritesNothingBack(Type editorType, object value)
    {
        var (window, editor, textBox, _, source) = Show(editorType, value);

        Assert.Equal(value, source.Value);
        Assert.Equal(0, source.Writes);
        Assert.False(string.IsNullOrEmpty(textBox.Text));
        window.Close();
    }

    [AvaloniaTheory]
    [MemberData(nameof(EditorsAndValues))]
    public void ShowingANewSourceValueWritesNothingBack(Type editorType, object value)
    {
        var (window, _, textBox, other, source) = Show(editorType, Activator.CreateInstance(value.GetType())!);

        source.SetFromCode(value);
        Dispatcher.UIThread.RunJobs();
        // Visiting the box without typing writes nothing either
        textBox.Focus();
        other.Focus();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(value, source.Value);
        Assert.Equal(0, source.Writes);
        window.Close();
    }

    [AvaloniaFact]
    public void TypingAColorThenEnterWritesIt()
    {
        var (window, _, textBox, _, source) = Show(typeof(Color3Editor), new Color3(1f, 0.5f, 0.25f));

        textBox.Focus();
        textBox.SelectAll();
        window.KeyTextInput("#204080");
        // Typing alone writes nothing
        Assert.Equal(0, source.Writes);
        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(new Color3(0x20 / 255f, 0x40 / 255f, 0x80 / 255f), source.Value);
        Assert.Equal(1, source.Writes);
        window.Close();
    }

    [AvaloniaFact]
    public void LeavingTheBoxAfterTypingWritesTheColor()
    {
        var (window, _, textBox, other, source) = Show(typeof(Color4Editor), new Color4(1f, 0.5f, 0.25f, 0.75f));

        textBox.Focus();
        textBox.SelectAll();
        window.KeyTextInput("#80402010");
        other.Focus();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(new Color4(0x40 / 255f, 0x20 / 255f, 0x10 / 255f, 0x80 / 255f), source.Value);
        Assert.Equal(1, source.Writes);
        window.Close();
    }

    [AvaloniaFact]
    public void EscapeGivesTheSourceValueBack()
    {
        var value = new Color3(1f, 0.5f, 0.25f);
        var (window, _, textBox, other, source) = Show(typeof(Color3Editor), value);
        var displayed = textBox.Text;

        textBox.Focus();
        textBox.SelectAll();
        window.KeyTextInput("#000000");
        window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        other.Focus();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(displayed, textBox.Text);
        Assert.Equal(value, source.Value);
        Assert.Equal(0, source.Writes);
        window.Close();
    }

    [AvaloniaFact]
    public void PickingAPaletteColorWritesIt()
    {
        var picked = new Color4(0.2f, 0.4f, 0.6f, 1f);
        var (window, editor, _, _, source) = Show(typeof(Color4Editor), new Color4(1f, 0.5f, 0.25f, 0.75f), new Dictionary<string, Color4> { ["Picked"] = picked });
        var toggle = editor.GetVisualDescendants().OfType<ToggleButton>().Single(x => x.Name == "PickerToggleButton");
        toggle.IsChecked = true;
        Dispatcher.UIThread.RunJobs();
        var popup = editor.GetVisualDescendants().OfType<Popup>().Single();
        popup.Child!.GetVisualDescendants().OfType<TabControl>().Single().SelectedIndex = 1;
        Dispatcher.UIThread.RunJobs();
        var palette = popup.Child!.GetVisualDescendants().OfType<ListBox>().Single();

        palette.SelectedIndex = 0;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(picked, source.Value);
        Assert.Equal(1, source.Writes);
        window.Close();
    }

    [AvaloniaFact]
    public void EveryPropertyHasItsOwnName()
    {
        Assert.Equal(nameof(Color4Editor.Palette), Color4Editor.PaletteProperty.Name);
        Assert.Equal(nameof(Color4Editor.SelectedPaletteColor), Color4Editor.SelectedPaletteColorProperty.Name);
    }

    private static (Window Window, TemplatedControl Editor, TextBox TextBox, TextBox Other, ValueSource Source) Show(Type editorType, object value, IReadOnlyDictionary<string, Color4>? palette = null)
    {
        var source = new ValueSource(value);
        var editor = (TemplatedControl)Activator.CreateInstance(editorType)!;
        editor.DataContext = source;
        if (palette is not null)
            editor.SetValue(Color4Editor.PaletteProperty, palette);
        // The binding a property grid uses: two-way, written when the editor's value changes
        editor.Bind(ValueProperty(editorType), new Binding(nameof(ValueSource.Value)) { Mode = BindingMode.TwoWay });
        var other = new TextBox();
        var window = new Window { Content = new StackPanel { Children = { editor, other } } };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var textBox = editor.GetVisualDescendants().OfType<TextBox>().Single(x => x.Name == "TextBox");
        return (window, editor, textBox, other, source);
    }

    private static global::Avalonia.AvaloniaProperty ValueProperty(Type editorType) => editorType == typeof(ColorEditor)
        ? ColorEditor.ValueProperty
        : editorType == typeof(Color3Editor) ? Color3Editor.ValueProperty : Color4Editor.ValueProperty;

    private sealed class ValueSource(object value) : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// The number of times the value was written with a different value.
        /// </summary>
        public int Writes { get; private set; }

        public object Value
        {
            get => Current;
            set
            {
                // Like a node of the property grid, which ignores a value equal to its own: a two-way binding on an object
                // property echoes the value it has just shown
                if (!Equals(Current, value))
                    Writes++;
                Current = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }

        private object Current { get; set; } = value;

        /// <summary>
        /// Changes the value as the model would, without counting it as a write: what the editor writes back while it
        /// shows the new value is counted.
        /// </summary>
        public void SetFromCode(object value)
        {
            Current = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
        }
    }
}

// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;
using Stride.Core.Presentation.Avalonia.Behaviors;

namespace Stride.Core.Presentation.Avalonia.Tests.Behaviors;

public sealed class CommitTextBehaviorTests
{
    [AvaloniaFact]
    public void ShowingANewSourceValueWritesNothingBack()
    {
        var (window, textBox, other, source) = Show();

        source.SetFromCode("displayed");
        Dispatcher.UIThread.RunJobs();
        textBox.Focus();
        other.Focus();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("displayed", textBox.Text);
        Assert.Equal(0, source.Writes);
        window.Close();
    }

    [AvaloniaFact]
    public void EnterCommitsAnEdit()
    {
        var (window, textBox, _, source) = Show();

        textBox.Focus();
        textBox.SelectAll();
        window.KeyTextInput("edited");
        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);

        Assert.Equal("edited", source.Value.Url);
        Assert.Equal(1, source.Writes);
        window.Close();
    }

    [AvaloniaFact]
    public void LeavingTheBoxCommitsAnEdit()
    {
        var (window, textBox, other, source) = Show();

        textBox.Focus();
        textBox.SelectAll();
        window.KeyTextInput("edited");
        other.Focus();

        Assert.Equal("edited", source.Value.Url);
        Assert.Equal(1, source.Writes);
        window.Close();
    }

    [AvaloniaFact]
    public void EscapeGivesTheSourceValueBackAndLeavingThenWritesNothing()
    {
        var (window, textBox, other, source) = Show();

        textBox.Focus();
        textBox.Text = "edited";
        window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        // TextBox raises TextChanged from the dispatcher, here while the box still has the focus: the text that Escape
        // restored must not count as an edit
        Dispatcher.UIThread.RunJobs();
        other.Focus();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("initial", textBox.Text);
        Assert.Equal("initial", source.Value.Url);
        Assert.Equal(0, source.Writes);
        window.Close();
    }

    private static (Window Window, TextBox TextBox, TextBox Other, Source Source) Show()
    {
        var source = new Source();
        var textBox = new TextBox();
        textBox.Bind(TextBox.TextProperty, new Binding(nameof(Source.Value)) { Source = source, Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.Explicit, Converter = new ReferenceToUrl() });
        Interaction.GetBehaviors(textBox).Add(new CommitTextBehavior());
        var other = new TextBox();
        var window = new Window { Content = new StackPanel { Children = { textBox, other } } };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, textBox, other, source);
    }

    /// <summary>
    /// A value that does not round-trip through its text, like a content reference: converting the text back makes a new
    /// object, so writing back the text the box shows is still a change for the source.
    /// </summary>
    private sealed class Reference(string url)
    {
        public string Url { get; } = url;
    }

    private sealed class ReferenceToUrl : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => (value as Reference)?.Url;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => new Reference(value as string ?? string.Empty);
    }

    private sealed class Source : INotifyPropertyChanged
    {
        private Reference value = new("initial");

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// The number of times the binding wrote the value back.
        /// </summary>
        public int Writes { get; private set; }

        public Reference Value
        {
            get => value;
            set
            {
                Writes++;
                this.value = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }

        /// <summary>
        /// Changes the value the way a view model does, which the binding then displays.
        /// </summary>
        public void SetFromCode(string url)
        {
            value = new Reference(url);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
        }
    }
}

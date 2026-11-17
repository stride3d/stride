// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;
using Stride.Core.Presentation.Avalonia.Behaviors;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Presentation.Avalonia.Tests.Behaviors;

public sealed class ValidateTextBehaviorTests
{
    [AvaloniaFact]
    public void TheBoxShowsItsInitialTextSelectedAndFocused()
    {
        var (window, textBox, _, _) = Show(_ => true);

        Assert.Equal("New key", textBox.Text);
        Assert.True(textBox.IsFocused);
        Assert.Equal("New key", textBox.SelectedText);
        window.Close();
    }

    [AvaloniaFact]
    public void EnterRunsTheCommandWithTheTextAndCloses()
    {
        var (window, _, behavior, run) = Show(_ => true);

        window.KeyTextInput("lamp");
        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);

        Assert.Equal(["lamp"], run);
        Assert.False(behavior.IsOpen);
        window.Close();
    }

    [AvaloniaFact]
    public void EnterClosesWithoutRunningACommandThatCannotRun()
    {
        var (window, _, behavior, run) = Show(text => text != "New key");

        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);

        Assert.Empty(run);
        Assert.False(behavior.IsOpen);
        window.Close();
    }

    [AvaloniaFact]
    public void EscapeClosesWithoutRunningTheCommand()
    {
        var (window, _, behavior, run) = Show(_ => true);

        window.KeyTextInput("lamp");
        window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);

        Assert.Empty(run);
        Assert.False(behavior.IsOpen);
        window.Close();
    }

    private static (Window Window, TextBox TextBox, ValidateTextBehavior Behavior, List<string> Run) Show(Func<string, bool> canRun)
    {
        var run = new List<string>();
        var command = new AnonymousCommand<string>(new ViewModelServiceProvider(), run.Add, canRun);
        var behavior = new ValidateTextBehavior { Command = command, InitialText = "New key", IsOpen = true };
        var textBox = new TextBox();
        Interaction.GetBehaviors(textBox).Add(behavior);
        var window = new Window { Content = new StackPanel { Children = { textBox, new TextBox() } } };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, textBox, behavior, run);
    }
}

// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Stride.Core.Assets.Editor.Avalonia.Tests.Helpers;
using Stride.Core.Assets.Editor.Avalonia.Views;

namespace Stride.Core.Assets.Editor.Avalonia.Tests;

/// <summary>
/// The editor of a <see cref="Guid"/>: the dashes come while typing, Enter writes a valid Guid once, and a text that is not a
/// Guid is shown in red and replaced by the value again.
/// </summary>
public sealed class GuidEditorTests
{
    private static readonly Guid Initial = new("0f8fad5b-d9cb-469f-a165-70867728950e");

    [AvaloniaFact]
    public void TheGuidIsShownInATextBox()
    {
        var (window, box, _) = Show();

        Assert.Equal(Initial.ToString(), box.Text);
        Assert.Equal(36, box.MaxLength);
        window.Close();
    }

    [AvaloniaFact]
    public void TypingAddsTheDashesAndEnterWritesTheGuidOnce()
    {
        var (window, box, holder) = Show();
        box.Focus();
        box.Text = string.Empty;
        // The box raises TextChanged from the dispatcher
        Dispatcher.UIThread.RunJobs();

        window.KeyTextInput("7c9e6679");
        Dispatcher.UIThread.RunJobs();
        // The dash after a full group comes with the last digit, and the caret is after it
        Assert.Equal("7c9e6679-", box.Text);
        Assert.Equal(9, box.CaretIndex);
        // Deleting the dash does not add it back
        window.KeyPressQwerty(PhysicalKey.Backspace, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("7c9e6679", box.Text);
        window.KeyTextInput("742540de944be07fc1f90ae7");
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("7c9e6679-7425-40de-944b-e07fc1f90ae7", box.Text);
        Assert.Equal(0, holder.Sets);

        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(new Guid("7c9e6679-7425-40de-944b-e07fc1f90ae7"), holder.Id);
        Assert.Equal(1, holder.Sets);
        window.Close();
    }

    [AvaloniaFact]
    public void ATextThatIsNotAGuidIsRedAndRevertedOnEnter()
    {
        var (window, box, holder) = Show();
        box.Focus();
        box.Text = string.Empty;
        Dispatcher.UIThread.RunJobs();

        window.KeyTextInput("12xz");
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(Brushes.IndianRed, box.Foreground);

        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(Initial.ToString(), box.Text);
        Assert.NotEqual(Brushes.IndianRed, box.Foreground);
        Assert.Equal(Initial, holder.Id);
        Assert.Equal(0, holder.Sets);
        window.Close();
    }

    [AvaloniaFact]
    public void ATextThatIsNotAGuidIsRevertedWhenTheFocusLeaves()
    {
        var (window, box, holder) = Show();
        box.Focus();
        box.Text = string.Empty;
        Dispatcher.UIThread.RunJobs();
        window.KeyTextInput("0f8fad5b");
        Dispatcher.UIThread.RunJobs();

        // An incomplete Guid
        window.GetVisualDescendants().OfType<TextBox>().Single(x => x.Name == "Other").Focus();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(Initial.ToString(), box.Text);
        Assert.Equal(0, holder.Sets);
        window.Close();
    }

    [AvaloniaFact]
    public void ThePastedFormsWithBracesAreAccepted()
    {
        var (window, box, holder) = Show();
        box.Focus();

        box.Text = "{7c9e6679-7425-40de-944b-e07fc1f90ae7}";
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("7c9e6679-7425-40de-944b-e07fc1f90ae7", box.Text);
        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(new Guid("7c9e6679-7425-40de-944b-e07fc1f90ae7"), holder.Id);
        window.Close();
    }

    private static (Window Window, TextBox Box, GuidHolder Holder) Show()
    {
        DefaultProviders.Register();
        var holder = new GuidHolder { Id = Initial };
        holder.ResetSets();
        var node = new NodeViewModelContext().MemberOf(holder, nameof(GuidHolder.Id));
        var editor = new ContentControl { Content = node, ContentTemplate = PropertyViewHelper.EditorProviders };
        // Another control to give the focus to
        var window = new Window { Width = 500, Height = 200, Content = new StackPanel { Children = { editor, new TextBox { Name = "Other" } } } };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var box = editor.GetVisualDescendants().OfType<TextBox>().Single(x => x.Name == "GuidTextBox");
        return (window, box, holder);
    }
}

public sealed class GuidHolder
{
    /// <summary>
    /// The number of times the Guid was set.
    /// </summary>
    public int Sets { get; private set; }

    public Guid Id
    {
        get;
        set
        {
            field = value;
            Sets++;
        }
    }

    public void ResetSets() => Sets = 0;
}

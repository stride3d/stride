// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;

using Stride.Core.Assets.Editor.Avalonia.Tests.Helpers;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.Tests;

/// <summary>
/// The editors of nullable value type members (float?, int?) accept an empty text and commit a null value.
/// On WPF, the empty text is not reported as an invalid value by the text box validation.
/// See https://github.com/stride3d/stride/issues/3014
/// </summary>
public sealed class NullableEditorTests
{
    [AvaloniaFact]
    public void ANullableFloatIsShownInAClearableEditor()
    {
        var holder = new NullableHolder { NullableFloat = 0.5f };
        var (window, box, node) = ShowEditor(holder, nameof(NullableHolder.NullableFloat));

        // The template provider decides whether the text box allows an empty value from the node type
        Assert.Equal(typeof(float?), node.Type);
        Assert.True(box.AllowNull);
        Assert.Equal(0.5, box.Value);
        Assert.Equal("0.5", Text(box));
        window.Close();
    }

    [AvaloniaFact]
    public void ANullableIntIsShownInAClearableEditor()
    {
        var holder = new NullableHolder { NullableInt = 3 };
        var (window, box, node) = ShowEditor(holder, nameof(NullableHolder.NullableInt));

        Assert.Equal(typeof(int?), node.Type);
        Assert.True(box.AllowNull);
        Assert.Equal(3, box.Value);
        Assert.Equal(0, box.DecimalPlaces);
        window.Close();
    }

    [AvaloniaFact]
    public void ClearingTheTextCommitsANullValue()
    {
        var holder = new NullableHolder { NullableFloat = 0.5f };
        var (window, box, _) = ShowEditor(holder, nameof(NullableHolder.NullableFloat));
        var textBox = box.GetVisualDescendants().OfType<TextBox>().Single();

        textBox.Focus();
        textBox.SelectAll();
        Dispatcher.UIThread.RunJobs();
        // The user deletes the selected text
        window.KeyPressQwerty(PhysicalKey.Delete, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        // The text box already reflects the null value, but the member is only updated on commit
        Assert.Null(box.Value);

        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.Null(holder.NullableFloat);
        Assert.Null(box.Value);
        Assert.Equal(string.Empty, Text(box));
        window.Close();
    }

    [AvaloniaFact]
    public void ANonNullableFloatIsNotClearable()
    {
        var holder = new NullableHolder { PlainFloat = 1.5f };
        var (window, box, node) = ShowEditor(holder, nameof(NullableHolder.PlainFloat));

        Assert.Equal(typeof(float), node.Type);
        Assert.False(box.AllowNull);

        var textBox = box.GetVisualDescendants().OfType<TextBox>().Single();
        textBox.Focus();
        textBox.SelectAll();
        Dispatcher.UIThread.RunJobs();
        // The user deletes the selected text
        window.KeyPressQwerty(PhysicalKey.Delete, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        // An empty text is not a valid value for a non-nullable member: the value is kept
        Assert.Equal(1.5, box.Value);

        // And the text is reverted to the value on commit
        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1.5f, holder.PlainFloat);
        Assert.Equal(1.5, box.Value);
        Assert.Equal("1.5", Text(box));
        window.Close();
    }

    private static (Window Window, NumericTextBox Box, NodeViewModel Node) ShowEditor(object holder, string member)
    {
        DefaultProviders.Register();
        var node = new NodeViewModelContext().MemberOf(holder, member);
        var window = new Window { Content = new ContentControl { Content = node, ContentTemplate = PropertyViewHelper.EditorProviders } };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, Assert.Single(window.GetVisualDescendants().OfType<NumericTextBox>()), node);
    }

    private static string? Text(NumericTextBox box) => box.GetVisualDescendants().OfType<TextBox>().First().Text;
}

public sealed class NullableHolder
{
    public float? NullableFloat { get; set; }
    public int? NullableInt { get; set; }
    public float PlainFloat { get; set; }
}

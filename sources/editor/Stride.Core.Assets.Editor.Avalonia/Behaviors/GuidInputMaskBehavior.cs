// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;

namespace Stride.Core.Assets.Editor.Avalonia.Behaviors;

/// <summary>
/// Live input mask for the text of a <see cref="Guid"/>: groups the hexadecimal digits as 8-4-4-4-12 while the user types,
/// adding the dashes (right after the last digit of a group, so the next digit starts the next group).
/// </summary>
/// <remarks>
/// Deleting is left alone: removing a dash does not add it back. A text that is not made of hexadecimal digits and dashes is
/// shown in red and not changed; when the user presses Enter or leaves the box with such a text, the box shows the bound
/// value again. Braces and parentheses around a pasted Guid are dropped.
/// </remarks>
public sealed class GuidInputMaskBehavior : Behavior<TextBox>
{
    private const int HexDigits = 32;
    private const int FullLength = 36; // 32 digits and 4 dashes
    private static readonly int[] GroupSizes = [8, 4, 4, 4, 12];

    private bool isUpdating;
    private string lastText = string.Empty;

    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject is not { } textBox)
            return;

        // A Guid has 36 characters: once full, the box takes no more typing (replacing a selection still works)
        textBox.MaxLength = FullLength;
        lastText = textBox.Text ?? string.Empty;
        textBox.TextChanged += OnTextChanged;
        // Before the behavior that commits the text
        textBox.AddHandler(InputElement.KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
        textBox.AddHandler(InputElement.LostFocusEvent, OnPreviewLostFocus, RoutingStrategies.Tunnel | RoutingStrategies.Direct | RoutingStrategies.Bubble);
    }

    protected override void OnDetaching()
    {
        if (AssociatedObject is { } textBox)
        {
            textBox.TextChanged -= OnTextChanged;
            textBox.RemoveHandler(InputElement.KeyDownEvent, OnPreviewKeyDown);
            textBox.RemoveHandler(InputElement.LostFocusEvent, OnPreviewLostFocus);
        }
        base.OnDetaching();
    }

    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            RevertInvalidText();
    }

    private void OnPreviewLostFocus(object? sender, RoutedEventArgs e)
    {
        RevertInvalidText();
    }

    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        // The box sets the text again after it raises the event for a typed text: change it once the input is handled
        if (!isUpdating)
            Dispatcher.UIThread.Post(ApplyMask);
    }

    private void ApplyMask()
    {
        if (AssociatedObject is not { } textBox)
            return;

        // The box can raise the event more than once for a change
        var text = textBox.Text ?? string.Empty;
        if (text == lastText)
            return;

        var grew = text.Length > lastText.Length;
        lastText = text;

        var raw = Raw(text);
        var valid = raw.Length <= HexDigits && IsHex(raw);
        SetInvalidHint(!valid);
        if (!valid)
            return;

        var formatted = Format(raw, appendTrailingDash: grew);
        if (formatted == text)
            return;

        var rawBeforeCaret = CountHexBefore(text, textBox.CaretIndex);
        isUpdating = true;
        try
        {
            // SetCurrentValue keeps the binding of the text
            textBox.SetCurrentValue(TextBox.TextProperty, formatted);
            textBox.CaretIndex = CaretAfter(formatted, rawBeforeCaret, grew);
            lastText = formatted;
        }
        finally
        {
            isUpdating = false;
        }
    }

    /// <summary>
    /// Shows the bound value again when the text is not a Guid, instead of writing it.
    /// </summary>
    private void RevertInvalidText()
    {
        if (AssociatedObject is not { } textBox || Guid.TryParse(textBox.Text, out _))
            return;

        // An empty box shows the placeholder of different values: nothing to revert
        if (string.IsNullOrEmpty(textBox.Text))
            return;

        BindingOperations.GetBindingExpressionBase(textBox, TextBox.TextProperty)?.UpdateTarget();
        SetInvalidHint(false);
    }

    /// <summary>
    /// Shows a text that is not a Guid in red while the user types, without changing it.
    /// </summary>
    private void SetInvalidHint(bool invalid)
    {
        if (AssociatedObject is not { } textBox)
            return;

        if (invalid)
            textBox.SetCurrentValue(TemplatedControl.ForegroundProperty, Brushes.IndianRed);
        else
            textBox.ClearValue(TemplatedControl.ForegroundProperty);
    }

    private static string Raw(string text) => text.Replace("-", "").Trim('{', '}', '(', ')', ' ');

    private static bool IsHex(string text)
    {
        foreach (var c in text)
        {
            if (!char.IsAsciiHexDigit(c))
                return false;
        }
        return true;
    }

    private static string Format(string raw, bool appendTrailingDash)
    {
        var result = new StringBuilder(raw.Length + 4);
        var taken = 0;
        foreach (var size in GroupSizes)
        {
            if (taken >= raw.Length)
                break;

            var count = Math.Min(size, raw.Length - taken);
            result.Append(raw, taken, count);
            taken += count;

            // A dash between groups when more digits follow; a trailing dash only when the text grew, so that deleting a
            // dash does not add it back
            var groupFull = count == size && taken < HexDigits;
            if (groupFull && (taken < raw.Length || appendTrailingDash))
                result.Append('-');
        }
        return result.ToString();
    }

    private static int CountHexBefore(string text, int caret)
    {
        var count = 0;
        for (var i = 0; i < caret && i < text.Length; i++)
        {
            if (text[i] != '-')
                count++;
        }
        return count;
    }

    private static int CaretAfter(string formatted, int rawBefore, bool grew)
    {
        int i = 0, seen = 0;
        while (i < formatted.Length && seen < rawBefore)
        {
            if (formatted[i] != '-')
                seen++;
            i++;
        }
        // After typing, step over the dash just added so that the next digit starts the next group
        if (grew)
        {
            while (i < formatted.Length && formatted[i] == '-')
                i++;
        }
        return i;
    }
}

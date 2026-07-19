// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;

namespace Stride.Core.Assets.Editor.View.Behaviors
{
    /// <summary>
    /// Live input mask for Guid text: re-groups hex input as 8-4-4-4-12 while typing, inserting
    /// the dashes automatically (including right after the 8th/12th/16th/20th hex digit, so the
    /// caret is already past the dash for the next group). Deletions are left alone — removing a
    /// dash with backspace doesn't fight the user by re-appending it. Input that isn't plain
    /// hex/dashes (braces, garbage) is left untouched; commit-time validation deals with it.
    /// </summary>
    public class GuidInputMaskBehavior : Behavior<TextBox>
    {
        private const int HexDigits = 32;
        private static readonly int[] GroupSizes = [8, 4, 4, 4, 12];

        private bool updating;
        private int previousLength;

        protected override void OnAttached()
        {
            base.OnAttached();
            previousLength = AssociatedObject.Text?.Length ?? 0;
            AssociatedObject.TextChanged += OnTextChanged;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.TextChanged -= OnTextChanged;
            base.OnDetaching();
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (updating)
                return;

            var box = AssociatedObject;
            var text = box.Text ?? string.Empty;
            var grew = text.Length > previousLength;
            previousLength = text.Length;

            var raw = text.Replace("-", "");
            if (raw.Length > HexDigits || !IsHex(raw))
                return;

            var formatted = Format(raw, appendTrailingDash: grew);
            if (formatted == text)
                return;

            var rawBeforeCaret = CountHexBefore(text, box.CaretIndex);

            updating = true;
            try
            {
                // SetCurrentValue keeps the Text binding alive (a plain Text= would clear it).
                box.SetCurrentValue(TextBox.TextProperty, formatted);
                box.CaretIndex = CaretAfter(formatted, rawBeforeCaret, grew);
                previousLength = formatted.Length;
            }
            finally
            {
                updating = false;
            }
        }

        private static bool IsHex(string s)
        {
            foreach (var c in s)
            {
                if (!char.IsAsciiHexDigit(c))
                    return false;
            }
            return true;
        }

        private static string Format(string raw, bool appendTrailingDash)
        {
            var result = new System.Text.StringBuilder(raw.Length + 4);
            int taken = 0;
            foreach (var size in GroupSizes)
            {
                if (taken >= raw.Length)
                    break;

                var count = System.Math.Min(size, raw.Length - taken);
                result.Append(raw, taken, count);
                taken += count;

                var groupFull = count == size && taken < HexDigits;
                // Dash between groups when more digits follow; trailing dash only on growth,
                // so backspacing over a dash doesn't immediately re-append it.
                if (groupFull && (taken < raw.Length || appendTrailingDash))
                    result.Append('-');
            }
            return result.ToString();
        }

        private static int CountHexBefore(string text, int caret)
        {
            int count = 0;
            for (int i = 0; i < caret && i < text.Length; i++)
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
            // After typing, hop over the dash we just inserted so the next digit starts the new group.
            if (grew)
            {
                while (i < formatted.Length && formatted[i] == '-')
                    i++;
            }
            return i;
        }
    }
}

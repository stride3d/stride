// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.DragDrop;

/// <summary>
/// Shows over a drop target, while something is dragged over it, where the drop would go and what it would do: the line
/// where the items would be inserted or the row that would take them, and the message of the target (why it refuses the
/// items, for instance). It draws in the adorner layer of the target and never takes the pointer.
/// </summary>
/// <remarks>
/// It shows the message of the target next to the cursor, and the insert and drop target marks of the trees.
/// </remarks>
public sealed class DragFeedbackAdorner : Control
{
    private static readonly IBrush AcceptedBrush = new ImmutableSolidColorBrush(Color.FromRgb(0x33, 0x99, 0xFF));
    private static readonly IBrush NoOpBrush = new ImmutableSolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99));
    private static readonly IBrush RejectedBrush = new ImmutableSolidColorBrush(Color.FromRgb(0xE0, 0x40, 0x40));
    private static readonly IBrush MessageBackground = new ImmutableSolidColorBrush(Color.FromArgb(0xE8, 0x20, 0x20, 0x20));
    private static readonly IBrush MessageForeground = Brushes.White;

    private DropAcceptance acceptance;
    private Rect? targetBounds;
    private (Point Start, Point End)? insertLine;
    private string message = string.Empty;
    private Point pointer;

    private DragFeedbackAdorner()
    {
        IsHitTestVisible = false;
        ClipToBounds = false;
    }

    /// <summary>
    /// Gets the message shown next to the pointer.
    /// </summary>
    public string Message => message;

    /// <summary>
    /// Gets how the target accepts the dragged items.
    /// </summary>
    public DropAcceptance Acceptance => acceptance;

    /// <summary>
    /// Gets the line where the dragged items would be inserted, relative to the adorned element, if any.
    /// </summary>
    public (Point Start, Point End)? InsertLine => insertLine;

    /// <summary>
    /// Gets the row that would take the dragged items, relative to the adorned element, if any.
    /// </summary>
    public Rect? TargetBounds => targetBounds;

    /// <summary>
    /// Gets the feedback shown over <paramref name="element"/>, if any.
    /// </summary>
    public static DragFeedbackAdorner? Get(Visual element)
    {
        return AdornerLayer.GetAdornerLayer(element)?.Children.OfType<DragFeedbackAdorner>().FirstOrDefault(x => ReferenceEquals(AdornerLayer.GetAdornedElement(x), element));
    }

    /// <summary>
    /// Shows the feedback of a drag over <paramref name="element"/>, replacing the one it showed.
    /// </summary>
    /// <param name="element">The drop target.</param>
    /// <param name="pointer">The pointer, relative to <paramref name="element"/>.</param>
    /// <param name="acceptance">How the target accepts the dragged items.</param>
    /// <param name="message">The message of the target.</param>
    /// <param name="targetBounds">The row that would take the items, relative to <paramref name="element"/>.</param>
    /// <param name="insertLine">The line where the items would be inserted, relative to <paramref name="element"/>.</param>
    public static void Show(Visual element, Point pointer, DropAcceptance acceptance, string message, Rect? targetBounds = null, (Point Start, Point End)? insertLine = null)
    {
        var adorner = Get(element);
        if (adorner is null)
        {
            if (AdornerLayer.GetAdornerLayer(element) is not { } layer)
                return;
            adorner = new DragFeedbackAdorner();
            AdornerLayer.SetAdornedElement(adorner, element);
            // The message goes past the target when the pointer is near its edge
            AdornerLayer.SetIsClipEnabled(adorner, false);
            layer.Children.Add(adorner);
        }

        adorner.pointer = pointer;
        adorner.acceptance = acceptance;
        adorner.message = message.Replace("\r\n", "\n");
        adorner.targetBounds = targetBounds;
        adorner.insertLine = insertLine;
        adorner.InvalidateVisual();
    }

    /// <summary>
    /// Removes the feedback shown over <paramref name="element"/>.
    /// </summary>
    public static void Clear(Visual element)
    {
        if (Get(element) is { } adorner && AdornerLayer.GetAdornerLayer(element) is { } layer)
            layer.Children.Remove(adorner);
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        var brush = acceptance switch
        {
            DropAcceptance.Accepted => AcceptedBrush,
            DropAcceptance.NoOp => NoOpBrush,
            _ => RejectedBrush,
        };

        if (insertLine is { } line)
        {
            var pen = new Pen(brush, 2);
            context.DrawLine(pen, line.Start, line.End);
            // Small ticks at the start of the insert line
            context.DrawLine(pen, line.Start + new Point(0, -3), line.Start + new Point(0, 3));
        }
        else if (targetBounds is { } bounds)
        {
            context.DrawRectangle(null, new Pen(brush, 1.5), bounds.Deflate(0.75), 2, 2);
        }

        if (string.IsNullOrEmpty(message))
            return;

        var text = new FormattedText(message, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, Typeface.Default, 12, MessageForeground);
        var origin = pointer + new Point(16, 16);
        var box = new Rect(origin, new Size(text.Width + 12, text.Height + 8));
        context.DrawRectangle(MessageBackground, new Pen(brush, 1), box, 3, 3);
        context.DrawText(text, origin + new Point(6, 4));
    }
}

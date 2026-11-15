// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using Stride.Core.Presentation.Drawing;

namespace Stride.Assets.Editor.ViewModels.CurveEditor;

/// <summary>
/// The axes a pan or a zoom of the curve editor applies to.
/// </summary>
/// <param name="X">Whether the horizontal axis (the keys) pans or zooms.</param>
/// <param name="Y">Whether the vertical axis (the values) pans or zooms.</param>
public readonly record struct CurveAxes(bool X, bool Y)
{
    /// <summary>
    /// Whether any axis pans or zooms.
    /// </summary>
    public bool Any => X || Y;

    /// <summary>
    /// The axes of a pan or a zoom from the modifier keys held, as the WPF view's behaviors: no key, both axes; Control, the
    /// horizontal one; Shift, the vertical one (both keys, both axes); any other key alone, none.
    /// </summary>
    /// <param name="shift">Whether Shift is held.</param>
    /// <param name="control">Whether Control is held.</param>
    /// <param name="other">Whether another modifier key (Alt, the Windows or Command key) is held.</param>
    public static CurveAxes FromModifiers(bool shift, bool control, bool other)
    {
        var none = !shift && !control && !other;
        return new CurveAxes(none || control, none || shift);
    }
}

partial class CurveEditorViewModel
{
    /// <summary>
    /// The factor of one step of the wheel when zooming in; zooming out uses its inverse.
    /// </summary>
    public const double ZoomInFactor = 1.25;

    /// <summary>
    /// Pans the view of the selected curve by the move of the pointer between two positions of the drawing.
    /// </summary>
    /// <param name="from">The previous position of the pointer.</param>
    /// <param name="to">The new position of the pointer.</param>
    /// <param name="axes">The axes that pan.</param>
    public void Pan(DrawingPoint from, DrawingPoint to, CurveAxes axes)
    {
        if (SelectedCurve is not { } curve || !axes.Any)
            return;

        if (axes.X)
            curve.XAxis?.Pan(from, to);
        if (axes.Y)
            curve.YAxis?.Pan(from, to);
        InvalidateView();
    }

    /// <summary>
    /// Zooms the view of the selected curve in or out, around a position of the drawing (the pointer).
    /// </summary>
    /// <param name="position">The position that stays in place.</param>
    /// <param name="zoomIn">Whether to zoom in (one step of the wheel up) or out.</param>
    /// <param name="axes">The axes that zoom.</param>
    public void Zoom(DrawingPoint position, bool zoomIn, CurveAxes axes)
    {
        if (SelectedCurve is not { } curve || !axes.Any)
            return;

        var factor = zoomIn ? ZoomInFactor : 1 / ZoomInFactor;
        var xAxis = curve.XAxis;
        var yAxis = curve.YAxis;
        // The position in the curve's space, as the WPF AxisZoomBehavior
        var current = xAxis is not null && yAxis is not null ? xAxis.InverseTransform(position.X, position.Y, yAxis)
            : yAxis is not null ? new DrawingPoint(0, yAxis.InverseTransform(position.Y))
            : new DrawingPoint();
        if (axes.X)
            xAxis?.ZoomAt(factor, current.X);
        if (axes.Y)
            yAxis?.ZoomAt(factor, current.Y);
        InvalidateView();
    }
}

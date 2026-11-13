// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Stride.Assets.Editor.ViewModels.CurveEditor;
using Stride.Core.Presentation.Avalonia.Drawing;
using Stride.Core.Presentation.Drawing;

namespace Stride.Assets.Editor.Avalonia.Views.CurveEditor;

/// <summary>
/// The view of the curve editor panel: the curves opened, and the selected one drawn with its axes in a
/// <see cref="Core.Presentation.Avalonia.Controls.DrawingCanvas"/>, where its keys are edited. The pointer input of the curve area
/// is in <see cref="CurveEditorPointerHandler"/>.
/// </summary>
public sealed partial class CurveEditorView : UserControl
{
    /// <summary>
    /// The display modes of a rotation curve, for its combo box.
    /// </summary>
    public static IReadOnlyList<RotationDisplayMode> RotationDisplayModes { get; } = Enum.GetValues<RotationDisplayMode>();

    /// <summary>
    /// Converts the curve area of the editor to the extents of the tracker.
    /// </summary>
    public static IValueConverter DrawingRectToRect { get; } = new FuncValueConverter<DrawingRect, Rect>(x => x.ToAvalonia());

    /// <summary>
    /// Whether a number of selected keys is not zero, for the commands that act on the selected keys.
    /// </summary>
    public static IValueConverter IsNotZero { get; } = new FuncValueConverter<int, bool>(x => x != 0);

    /// <summary>
    /// Whether a curve has keys to edit, for the key toolbar and the menu items that edit keys.
    /// </summary>
    public static IValueConverter IsEditable { get; } = new FuncValueConverter<object?, bool>(x => x is IEditableCurveViewModel);

    private readonly CurveEditorPointerHandler pointerHandler;

    public CurveEditorView()
    {
        AvaloniaXamlLoader.Load(this);
        pointerHandler = new CurveEditorPointerHandler(this.Get<Control>("Canvas"), this.Get<Control>("CurveArea"), this.Get<Control>("SelectionRectangle"),
            () => DataContext as CurveEditorViewModel);
        // The context menu's "Add point" adds the key where the menu was opened
        this.Get<ContextMenu>("CurveContextMenu").Opening += (_, _) => this.Get<MenuItem>("AddPointMenuItem").CommandParameter = pointerHandler.LastRightClickPosition;
    }
}

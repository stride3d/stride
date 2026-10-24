// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Avalonia.Controls;

namespace Stride.Core.Presentation.Avalonia.Tests.Controls;

public sealed class EditorControlTests
{
    private static T Show<T>(T control)
        where T : Control
    {
        var window = new Window { Content = control };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return control;
    }

    public static TheoryData<Type, int> EditorsAndNumericBoxCount => new()
    {
        { typeof(Vector2Editor), 2 },
        { typeof(Vector3Editor), 3 },
        { typeof(Vector4Editor), 4 },
        { typeof(Int2Editor), 2 },
        { typeof(Int3Editor), 3 },
        { typeof(Int4Editor), 4 },
        { typeof(RectangleEditor), 4 },
        { typeof(RectangleFEditor), 4 },
        { typeof(QuaternionEditor), 3 },
        { typeof(MatrixEditor), 16 },
    };

    [AvaloniaTheory]
    [MemberData(nameof(EditorsAndNumericBoxCount))]
    public void EditorStylesTemplateEveryEditor(Type editorType, int numericBoxCount)
    {
        var editor = Show((TemplatedControl)Activator.CreateInstance(editorType)!);

        Assert.NotNull(editor.Template);
        Assert.Equal(numericBoxCount, editor.GetVisualDescendants().OfType<NumericTextBox>().Count());
    }

    [AvaloniaTheory]
    [InlineData(typeof(ColorEditor))]
    [InlineData(typeof(Color3Editor))]
    [InlineData(typeof(Color4Editor))]
    [InlineData(typeof(DateTimeEditor))]
    [InlineData(typeof(TimeSpanEditor))]
    public void EditorStylesTemplateOtherEditors(Type editorType)
    {
        var editor = Show((TemplatedControl)Activator.CreateInstance(editorType)!);

        Assert.NotNull(editor.Template);
        Assert.NotEmpty(editor.GetVisualChildren());
    }

    [AvaloniaFact]
    public void Vector3EditorSplitsTheValueIntoComponents()
    {
        var editor = Show(new Vector3Editor { Value = new Vector3(1, 2, 3) });

        Assert.Equal(1f, editor.X);
        Assert.Equal(2f, editor.Y);
        Assert.Equal(3f, editor.Z);

        var boxes = editor.GetVisualDescendants().OfType<NumericTextBox>().ToList();
        Assert.Equal([1.0, 2.0, 3.0], boxes.Select(x => x.Value));
    }

    [AvaloniaFact]
    public void Vector3EditorComponentsUpdateTheValue()
    {
        var editor = Show(new Vector3Editor { Value = new Vector3(1, 2, 3) });

        editor.Y = 5;

        Assert.Equal(new Vector3(1, 5, 3), editor.Value);
    }

    [AvaloniaFact]
    public void Vector3EditorAllComponentsModeSetsEveryComponent()
    {
        var editor = Show(new Vector3Editor { EditingMode = VectorEditingMode.AllComponents, Value = new Vector3(1, 2, 3) });

        editor.X = 4;

        Assert.Equal(new Vector3(4), editor.Value);
        Assert.Equal(4f, editor.Z);
    }

    [AvaloniaFact]
    public void Vector3EditorLengthModeScalesTheVector()
    {
        var editor = Show(new Vector3Editor { EditingMode = VectorEditingMode.Length, Value = new Vector3(3, 0, 4) });

        Assert.Equal(5f, editor.Length);

        editor.Length = 10;

        Assert.Equal(new Vector3(6, 0, 8), editor.Value);
    }

    [AvaloniaFact]
    public void DecimalPlacesRoundTheComponents()
    {
        var editor = Show(new Vector2Editor { DecimalPlaces = 1, Value = new Vector2(1.26f, 2.0f) });

        Assert.Equal(1.3f, editor.X!.Value, 3);
    }

    [AvaloniaFact]
    public void ResetValueRestoresTheDefaultValue()
    {
        var editor = Show(new Vector3Editor { DefaultValue = Vector3.One, Value = new Vector3(1, 2, 3) });

        editor.ResetValue();

        Assert.Equal(Vector3.One, editor.Value);
    }

    [AvaloniaFact]
    public void SetVectorFromValueFillsEveryComponent()
    {
        var editor = Show(new Vector4Editor { Value = Vector4.Zero });

        editor.SetVectorFromValue(2);

        Assert.Equal(new Vector4(2), editor.Value);
    }

    [AvaloniaFact]
    public void Int3EditorSplitsTheValueIntoComponents()
    {
        var editor = Show(new Int3Editor { Value = new Int3(1, 2, 3) });

        Assert.Equal(1, editor.X);
        Assert.Equal(2, editor.Y);
        Assert.Equal(3, editor.Z);

        editor.Z = 7;

        Assert.Equal(new Int3(1, 2, 7), editor.Value);
    }

    [AvaloniaFact]
    public void Vector2EditorLengthModeScalesTheVector()
    {
        var editor = Show(new Vector2Editor { EditingMode = VectorEditingMode.Length, Value = new Vector2(3, 4) });

        Assert.Equal(5f, editor.Length);

        editor.Length = 10;

        Assert.Equal(new Vector2(6, 8), editor.Value);
    }

    [AvaloniaFact]
    public void Vector4EditorLengthModeScalesTheVector()
    {
        var editor = Show(new Vector4Editor { EditingMode = VectorEditingMode.Length, Value = new Vector4(1, 1, 1, 1) });

        Assert.Equal(2f, editor.Length);

        editor.Length = 4;

        Assert.Equal(new Vector4(2), editor.Value);
    }

    [AvaloniaFact]
    public void Vector4EditorComponentsUpdateTheValue()
    {
        var editor = Show(new Vector4Editor { Value = new Vector4(1, 2, 3, 4) });

        editor.Z = 5;
        editor.W = 6;

        Assert.Equal(new Vector4(1, 2, 5, 6), editor.Value);
    }

    [AvaloniaFact]
    public void RectangleEditorComponentsUpdateTheValue()
    {
        var editor = Show(new RectangleEditor { Value = new Rectangle(1, 2, 3, 4) });

        Assert.Equal([1, 2, 3, 4], new[] { editor.RectX, editor.RectY, editor.RectWidth, editor.RectHeight });

        editor.RectWidth = 30;
        Assert.Equal(new Rectangle(1, 2, 30, 4), editor.Value);

        editor.RectHeight = 40;
        Assert.Equal(new Rectangle(1, 2, 30, 40), editor.Value);
    }

    [AvaloniaFact]
    public void RectangleFEditorComponentsUpdateTheValue()
    {
        var editor = Show(new RectangleFEditor { Value = new RectangleF(1, 2, 3, 4) });

        Assert.Equal([1f, 2f, 3f, 4f], new[] { editor.RectX, editor.RectY, editor.RectWidth, editor.RectHeight });

        editor.RectWidth = 30;
        Assert.Equal(new RectangleF(1, 2, 30, 4), editor.Value);
    }

    [AvaloniaFact]
    public void MatrixEditorComponentsUpdateTheValue()
    {
        var editor = Show(new MatrixEditor { Value = Matrix.Identity });

        Assert.Equal(1f, editor.M11);
        Assert.Equal(0f, editor.M12);

        editor.M43 = 5;

        Assert.Equal(5f, editor.Value!.Value.M43);
        Assert.Equal(1f, editor.Value!.Value.M44);
    }

    [AvaloniaFact]
    public void ComponentPropertiesAreOwnedByTheirEditor()
    {
        Assert.Equal(typeof(RectangleFEditor), RectangleFEditor.RectXProperty.OwnerType);
        Assert.Equal(typeof(MatrixEditor), MatrixEditor.M11Property.OwnerType);
        Assert.Equal(typeof(Vector4Editor), Vector4Editor.WProperty.OwnerType);
        Assert.Equal(typeof(Int3Editor), Int3Editor.ZProperty.OwnerType);
        Assert.Equal(typeof(TimeSpanEditor), TimeSpanEditor.PlaceholderTextProperty.OwnerType);
    }
}

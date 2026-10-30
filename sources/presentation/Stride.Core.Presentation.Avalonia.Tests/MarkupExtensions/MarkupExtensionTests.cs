// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Input;
using Stride.Core.Presentation.Avalonia.MarkupExtensions;

namespace Stride.Core.Presentation.Avalonia.Tests.MarkupExtensions;

public sealed class MarkupExtensionTests
{
    [Fact]
    public void ConstantExtensions()
    {
        Assert.Equal(true, new TrueExtension().ProvideValue(null!));
        Assert.Equal(false, new FalseExtension().ProvideValue(null!));
        Assert.Equal(int.MaxValue, new MaxIntExtension().ProvideValue(null!));
        Assert.Equal(int.MinValue, new MinIntExtension().ProvideValue(null!));
        Assert.Equal(double.MaxValue, new MaxDoubleExtension().ProvideValue(null!));
        Assert.Equal(double.MinValue, new MinDoubleExtension().ProvideValue(null!));
#if DEBUG
        Assert.Equal(true, new IsDebug().ProvideValue(null!));
#else
        Assert.Equal(false, new IsDebug().ProvideValue(null!));
#endif
    }

    [Fact]
    public void NumericExtensionsParseInvariantStrings()
    {
        Assert.Equal(1.5, new DoubleExtension("1.5").ProvideValue(null!));
        Assert.Equal(0.0, new DoubleExtension().ProvideValue(null!));
        Assert.Equal(42, new IntExtension("42").ProvideValue(null!));
        Assert.Equal(0, new IntExtension().ProvideValue(null!));
    }

    [Fact]
    public void GuidExtensionParsesOrFallsBackToEmpty()
    {
        var guid = Guid.NewGuid();
        Assert.Equal(guid, new GuidExtension(guid.ToString()).ProvideValue(null!));
        Assert.Equal(Guid.Empty, new GuidExtension("not a guid").ProvideValue(null!));
    }

    [Fact]
    public void ThicknessExtensionConstructors()
    {
        Assert.Equal(new Thickness(2), new ThicknessExtension(2).ProvideValue(null!));
        Assert.Equal(new Thickness(1, 2, 1, 2), new ThicknessExtension(1, 2).ProvideValue(null!));
        Assert.Equal(new Thickness(1, 2, 3, 4), new ThicknessExtension(1, 2, 3, 4).ProvideValue(null!));
        Assert.Equal(new Thickness(0), new ThicknessExtension().ProvideValue(null!));
    }

    [Fact]
    public void SizeExtensionConstructors()
    {
        Assert.Equal(new Size(3, 3), new SizeExtension(3).ProvideValue(null!));
        Assert.Equal(new Size(3, 4), new SizeExtension(3, 4).ProvideValue(null!));
        Assert.Equal(Size.Infinity, new SizeExtension().ProvideValue(null!));
    }

    [Fact]
    public void KeyExtensionParsesKeyNamesIgnoringCase()
    {
        Assert.Equal(Key.F5, new KeyExtension("f5").ProvideValue(null!));
        Assert.Equal(Key.None, new KeyExtension().ProvideValue(null!));
    }

    [Fact]
    public void KeyGestureExtensionParsesModifiers()
    {
        var gesture = Assert.IsType<KeyGesture>(new KeyGestureExtension("Ctrl+Shift+S").ProvideValue(null!));
        Assert.Equal(Key.S, gesture.Key);
        Assert.Equal(KeyModifiers.Control | KeyModifiers.Shift, gesture.KeyModifiers);
    }

    [Fact]
    public void ToolTipExtensionAppendsTheGesture()
    {
        Assert.Equal("Save (Ctrl+S)", new ToolTipExtension("Save", "Ctrl+S").ProvideValue(null!));
        Assert.Equal("Save", new ToolTipExtension("Save", "").ProvideValue(null!));
    }

    [Fact]
    public void LocalizeStringExtensionReturnsTheTextWithoutTranslation()
    {
        Assert.Equal("Hello", new LocalizeStringExtension("Hello").ProvideValue(null!));
        Assert.Equal(string.Empty, new LocalizeStringExtension().ProvideValue(null!));
    }
}

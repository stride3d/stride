// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Avalonia;
using Stride.Core.Presentation.Avalonia.Converters;

namespace Stride.Core.Presentation.Avalonia.Tests.Converters;

public sealed class MultiValueConverterTests
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    [Fact]
    public void ProvideValueReturnsASharedInstance()
    {
        Assert.Same(new AndMulti().ProvideValue(null!), new AndMulti().ProvideValue(null!));
    }

    [Theory]
    [InlineData(true, true, true, true, false)]
    [InlineData(true, false, false, true, true)]
    [InlineData(false, false, false, false, false)]
    public void BooleanMultiConverters(bool first, bool second, bool and, bool or, bool xor)
    {
        object?[] values = [first, second];
        Assert.Equal(and, new AndMulti().Convert(values, typeof(bool), null, Culture));
        Assert.Equal(or, new OrMulti().Convert(values, typeof(bool), null, Culture));
        Assert.Equal(xor, new XOrMulti().Convert(values, typeof(bool), null, Culture));
    }

    [Fact]
    public void BooleanMultiConvertersNeedTwoValues()
    {
        object?[] values = [true];
        Assert.Same(AvaloniaProperty.UnsetValue, new AndMulti().Convert(values, typeof(bool), null, Culture));
        Assert.Same(AvaloniaProperty.UnsetValue, new OrMulti().Convert(values, typeof(bool), null, Culture));
        Assert.Same(AvaloniaProperty.UnsetValue, new XOrMulti().Convert(values, typeof(bool), null, Culture));
        Assert.Same(AvaloniaProperty.UnsetValue, new AllEqualMulti().Convert(values, typeof(bool), null, Culture));
    }

    [Fact]
    public void AllEqualMultiUsesTheParameterForUnsetValues()
    {
        var converter = new AllEqualMulti();
        Assert.Equal(true, converter.Convert([1, 1, 1], typeof(bool), null, Culture));
        Assert.Equal(false, converter.Convert([1, 2], typeof(bool), null, Culture));
        Assert.Equal(false, converter.Convert([1, AvaloniaProperty.UnsetValue], typeof(bool), null, Culture));
        Assert.Equal(true, converter.Convert([1, AvaloniaProperty.UnsetValue], typeof(bool), true, Culture));
    }

    [Fact]
    public void PriorityMultiReturnsTheFirstSetValue()
    {
        var converter = new PriorityMulti();
        Assert.Equal("b", converter.Convert([AvaloniaProperty.UnsetValue, null, "b", "c"], typeof(object), null, Culture));
        Assert.Null(converter.Convert([AvaloniaProperty.UnsetValue, null, "b"], typeof(object), true, Culture));
        Assert.Same(AvaloniaProperty.UnsetValue, converter.Convert([AvaloniaProperty.UnsetValue], typeof(object), true, Culture));
    }

    [Fact]
    public void MultiToTupleBuildsATupleOfTheValues()
    {
        var converter = new MultiToTuple();
        Assert.Equal(Tuple.Create<object?, object?>(1, "a"), converter.Convert([1, "a"], typeof(object), null, Culture));
        Assert.Throws<ArgumentException>(() => converter.Convert([], typeof(object), null, Culture));
    }
}

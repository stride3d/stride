// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Avalonia;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Avalonia.Converters;

namespace Stride.Core.Presentation.Avalonia.Tests.Converters;

public sealed class ValueConverterTests
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    private enum TestEnum
    {
        Plain,
        [Display("Pretty name")]
        Decorated,
    }

    [Fact]
    public void ProvideValueReturnsASharedInstance()
    {
        var first = new InvertBool().ProvideValue(null!);
        var second = new InvertBool().ProvideValue(null!);
        Assert.Same(first, second);
    }

    [Fact]
    public void ConverterGenericArgumentMustBeTheImplementingType()
    {
        Assert.Throws<InvalidOperationException>(() => new WrongConverter());
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void InvertBoolInvertsBothWays(bool value, bool expected)
    {
        var converter = new InvertBool();
        Assert.Equal(expected, converter.Convert(value, typeof(bool), null, Culture));
        Assert.Equal(expected, converter.ConvertBack(value, typeof(bool), null, Culture));
    }

    [Theory]
    [InlineData(true, 1.0)]
    [InlineData(false, 0.5)]
    [InlineData(null, 0.5)]
    public void BoolToOpacityDimsFalseValues(bool? value, double expected)
    {
        Assert.Equal(expected, new BoolToOpacity().Convert(value, typeof(double), null, Culture));
    }

    [Fact]
    public void OneWayConvertersRejectConvertBack()
    {
        Assert.Throws<NotSupportedException>(() => new BoolToOpacity().ConvertBack(1.0, typeof(bool), null, Culture));
    }

    [Fact]
    public void BoolToParamReturnsTheParameterOrUnset()
    {
        var converter = new BoolToParam();
        Assert.Equal("yes", converter.Convert(true, typeof(object), "yes", Culture));
        Assert.Same(AvaloniaProperty.UnsetValue, converter.Convert(false, typeof(object), "yes", Culture));
        Assert.Equal(true, converter.ConvertBack("yes", typeof(bool), null, Culture));
        Assert.Equal(false, converter.ConvertBack(AvaloniaProperty.UnsetValue, typeof(bool), null, Culture));
    }

    [Theory]
    [InlineData(2.0, 1.0, true, false, true, false, false, true)]
    [InlineData(1.0, 2.0, false, true, false, true, false, true)]
    [InlineData(1.0, 1.0, false, false, true, true, true, false)]
    public void CompareNumConverters(double value, double parameter, bool greater, bool lower, bool greaterOrEqual, bool lowerOrEqual, bool equal, bool different)
    {
        Assert.Equal(greater, new IsGreater().Convert(value, typeof(bool), parameter, Culture));
        Assert.Equal(lower, new IsLower().Convert(value, typeof(bool), parameter, Culture));
        Assert.Equal(greaterOrEqual, new IsGreaterOrEqual().Convert(value, typeof(bool), parameter, Culture));
        Assert.Equal(lowerOrEqual, new IsLowerOrEqual().Convert(value, typeof(bool), parameter, Culture));
        Assert.Equal(equal, new IsEqual().Convert(value, typeof(bool), parameter, Culture));
        Assert.Equal(different, new IsDifferent().Convert(value, typeof(bool), parameter, Culture));
    }

    [Fact]
    public void CompareNumParsesStringParameters()
    {
        Assert.Equal(true, new IsGreater().Convert(3, typeof(bool), "2.5", Culture));
    }

    [Fact]
    public void NumericToThicknessScalesTheParameter()
    {
        var converter = new NumericToThickness();
        var result = converter.Convert(2, typeof(Thickness), new Thickness(1, 2, 3, 4), Culture);
        Assert.Equal(new Thickness(2, 4, 6, 8), result);
        Assert.Equal(2.0, converter.ConvertBack(new Thickness(2, 4, 6, 8), typeof(double), new Thickness(1, 2, 3, 4), Culture));
        Assert.Throws<ArgumentException>(() => converter.Convert(2, typeof(Thickness), "not a thickness", Culture));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(3, true)]
    [InlineData(-1L, true)]
    [InlineData((byte)0, false)]
    public void NumericToBoolIsTrueForNonZeroValues(object value, bool expected)
    {
        Assert.Equal(expected, new NumericToBool().Convert(value, typeof(bool), null, Culture));
    }

    [Fact]
    public void NumericToBoolRejectsNonIntegralValues()
    {
        Assert.Throws<ArgumentException>(() => new NumericToBool().Convert(1.5, typeof(bool), null, Culture));
    }

    [Fact]
    public void ToDoubleConvertsBothWays()
    {
        var converter = new ToDouble();
        Assert.Equal(42.0, converter.Convert(42, typeof(double), null, Culture));
        Assert.Equal(42, converter.ConvertBack(42.0, typeof(int), null, Culture));
        Assert.Equal(1.5f, converter.ConvertBack(1.5, typeof(float), null, Culture));
    }

    [Fact]
    public void CharToStringConvertsBothWays()
    {
        var converter = new CharToString();
        Assert.Equal("a", converter.Convert('a', typeof(string), null, Culture));
        Assert.Equal(string.Empty, converter.Convert(null, typeof(string), null, Culture));
        Assert.Equal('b', converter.ConvertBack("bc", typeof(char), null, Culture));
        Assert.Equal('\0', converter.ConvertBack(string.Empty, typeof(char), null, Culture));
        Assert.Null(converter.ConvertBack(string.Empty, typeof(char?), null, Culture));
    }

    [Fact]
    public void CharToUnicodeConvertsBothWays()
    {
        var converter = new CharToUnicode();
        Assert.Equal(65, converter.Convert('A', typeof(int), null, Culture));
        Assert.Equal('A', converter.ConvertBack(65, typeof(char), null, Culture));
    }

    [Fact]
    public void ObjectToBoolTestsForNull()
    {
        Assert.Equal(true, new ObjectToBool().Convert(new object(), typeof(bool), null, Culture));
        Assert.Equal(false, new ObjectToBool().Convert(null, typeof(bool), null, Culture));
    }

    [Fact]
    public void IsEqualToParamUsesValueEqualityForValueTypesAndStrings()
    {
        var converter = new IsEqualToParam();
        Assert.Equal(true, converter.Convert(3, typeof(bool), 3, Culture));
        Assert.Equal(true, converter.Convert("abc", typeof(bool), new string("abc".ToCharArray()), Culture));
        Assert.Equal(true, converter.Convert(null, typeof(bool), null, Culture));
        Assert.Equal(false, converter.Convert(new List<int>(), typeof(bool), new List<int>(), Culture));
    }

    [Fact]
    public void AngleSingleToDegreesConvertsBothWays()
    {
        var converter = new AngleSingleToDegrees();
        var degrees = System.Convert.ToDouble(converter.Convert(new AngleSingle(MathUtil.PiOverTwo, AngleType.Radian), typeof(double), null, Culture));
        Assert.Equal(90.0, degrees, 3);
        var angle = Assert.IsType<AngleSingle>(converter.ConvertBack(180.0, typeof(AngleSingle), null, Culture));
        Assert.Equal(MathUtil.Pi, angle.Radians, 3);
    }

    [Fact]
    public void UFileToStringUsesThePlatformSeparator()
    {
        var converter = new UFileToString();
        var text = converter.Convert(new UFile("folder/file.txt"), typeof(string), null, Culture);
        Assert.Equal($"folder{Path.DirectorySeparatorChar}file.txt", text);
        Assert.Equal(new UFile("folder/file.txt"), converter.ConvertBack("folder/file.txt", typeof(UFile), null, Culture));
    }

    [Fact]
    public void YieldWrapsTheValueInASequence()
    {
        var result = Assert.IsAssignableFrom<IEnumerable<object?>>(new Yield().Convert(5, typeof(object), null, Culture));
        Assert.Equal([5], result);
        Assert.Same(AvaloniaProperty.UnsetValue, new Yield().Convert(AvaloniaProperty.UnsetValue, typeof(object), null, Culture));
    }

    [Fact]
    public void EnumToDisplayNameReadsTheDisplayAttribute()
    {
        var converter = new EnumToDisplayName();
        Assert.Equal("Plain", converter.Convert(TestEnum.Plain, typeof(string), null, Culture));
        Assert.Equal("Pretty name", converter.Convert(TestEnum.Decorated, typeof(string), null, Culture));
        Assert.Equal("(None)", converter.Convert(null, typeof(string), null, Culture));
    }

    [Fact]
    public void ObjectToTypeNameUsesTheSimpleCSharpName()
    {
        var converter = new ObjectToTypeName();
        Assert.Equal("int", converter.Convert(1, typeof(string), null, Culture));
        Assert.Equal(ObjectToTypeName.NullObjectType, converter.Convert(null, typeof(string), null, Culture));
    }

    [Fact]
    public void ChainedAppliesConvertersInOrderAndBackwards()
    {
        var chained = new Chained(new InvertBool(), new BoolToParam()) { Parameter2 = "visible" };
        Assert.Equal("visible", chained.Convert(false, typeof(object), null, Culture));
        Assert.Same(AvaloniaProperty.UnsetValue, chained.Convert(true, typeof(object), null, Culture));
        // BoolToParam back gives true, InvertBool back gives false.
        Assert.Equal(false, chained.ConvertBack("visible", typeof(bool), null, Culture));
    }

    [Fact]
    public void ChainedRejectsGapsBetweenConverters()
    {
        var chained = new Chained { Converter1 = new InvertBool(), Converter3 = new InvertBool() };
        Assert.Throws<InvalidOperationException>(() => chained.Convert(true, typeof(bool), null, Culture));
    }

    private sealed class WrongConverter : OneWayValueConverter<InvertBoolLike>
    {
        public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value;
    }

    private sealed class InvertBoolLike : OneWayValueConverter<InvertBoolLike>
    {
        public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value;
    }
}

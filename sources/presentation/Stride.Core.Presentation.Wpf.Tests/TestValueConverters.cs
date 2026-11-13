// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Stride.Core.Presentation.ValueConverters;
using Xunit;

namespace Stride.Core.Presentation.Tests
{
    public class TestValueConverters
    {
        [Fact]
        public void TestObjectToTypeNameConvertsNullToNone()
        {
            var converter = new ObjectToTypeName();

            Assert.Equal(converter.Convert(null, typeof(string), null, CultureInfo.CurrentCulture), ObjectToTypeName.NullObjectType);
        }

        [Fact]
        public void TestObjectToTypeNameConverterValueToType()
        {
            var converter = new ObjectToTypeName();

            Assert.NotEqual(converter.Convert("hello", typeof(string), null, CultureInfo.CurrentCulture), ObjectToTypeName.NullObjectType);
        }

        [Theory]
        [InlineData(true, true, true)]
        [InlineData(true, false, false)]
        [InlineData(false, false, false)]
        public void TestAndMultiConverterCombinesBooleans(bool first, bool second, bool expected)
        {
            Assert.Equal(expected, ConvertMulti(new AndMultiConverter(), null, first, second));
        }

        [Fact]
        public void TestAndMultiConverterReadsAnotherValueAsFalse()
        {
            // A value that is not a boolean counts as false instead of throwing
            Assert.False(ConvertMulti(new AndMultiConverter(), null, true, "text"));
            Assert.False(ConvertMulti(new AndMultiConverter(), null, true, null));
        }

        [Fact]
        public void TestAndMultiConverterUsesTheParameterForAnUnsetValue()
        {
            Assert.True(ConvertMulti(new AndMultiConverter(), true, true, DependencyProperty.UnsetValue));
            Assert.False(ConvertMulti(new AndMultiConverter(), null, true, DependencyProperty.UnsetValue));
        }

        [Theory]
        [InlineData(true, false, true)]
        [InlineData(false, false, false)]
        public void TestOrMultiConverterCombinesBooleans(bool first, bool second, bool expected)
        {
            Assert.Equal(expected, ConvertMulti(new OrMultiConverter(), null, first, second));
        }

        [Fact]
        public void TestOrMultiConverterReadsAnotherValueAsFalse()
        {
            Assert.False(ConvertMulti(new OrMultiConverter(), null, false, DependencyProperty.UnsetValue));
            Assert.False(ConvertMulti(new OrMultiConverter(), null, false, "text"));
            Assert.True(ConvertMulti(new OrMultiConverter(), null, null, true));
        }

        [Theory]
        [InlineData(true, true, false)]
        [InlineData(true, false, true)]
        [InlineData(false, false, false)]
        public void TestXOrMultiConverterCombinesBooleans(bool first, bool second, bool expected)
        {
            Assert.Equal(expected, ConvertMulti(new XOrMultiConverter(), null, first, second));
        }

        [Fact]
        public void TestXOrMultiConverterReadsAnotherValueAsFalse()
        {
            Assert.True(ConvertMulti(new XOrMultiConverter(), null, true, null));
            Assert.True(ConvertMulti(new XOrMultiConverter(), null, "text", true));
            Assert.False(ConvertMulti(new XOrMultiConverter(), null, DependencyProperty.UnsetValue, "text"));
        }

        private static bool ConvertMulti(System.Windows.Data.IMultiValueConverter converter, object parameter, params object[] values)
        {
            return (bool)converter.Convert(values, typeof(bool), parameter, CultureInfo.InvariantCulture);
        }
    }
}

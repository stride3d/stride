// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }
}

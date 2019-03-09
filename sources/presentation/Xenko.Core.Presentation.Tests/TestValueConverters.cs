// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xenko.Core.Presentation.ValueConverters;
using Xunit;

namespace Xenko.Core.Presentation.Tests
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

        [Fact]
        public void TestTypeNameHelperReturnsCorrectlyFormattedName()
        {
            var tests = new List<(Type Type, string Name)>()
            {
                (typeof(int), "int"), //Simple type
                (typeof(int?), "int?"), //Nullable Simple type
                (typeof(TimeSpan), "TimeSpan"), //Type
                (typeof(TimeSpan?), "TimeSpan?"), //Nullable type
                (typeof(int[]), "int[]"), //Array of Simple type
                (typeof(int?[]), "int?[]"), //Array of Nullable Simple type
                (typeof(TimeSpan[]), "TimeSpan[]"), //Array of Type
                (typeof(TimeSpan?[]), "TimeSpan?[]"), //Array of Nullable type
                (typeof(TimeSpan[,,]), "TimeSpan[,,]"), //Multi-dimesional Array of Type
                (typeof(Dictionary<string, FactAttribute>), "Dictionary<string, FactAttribute>"), //Generic type
                (typeof((string, FactAttribute)), "(string, FactAttribute)"), //Tuple Types
                (typeof((string, FactAttribute)?), "(string, FactAttribute)?"), //Nullable Tuple Types
                (typeof(Dictionary<string, GenericStruct<int?>>), "Dictionary<string, GenericStruct<int?>>"), //Nested Grneric Type
                (typeof((GenericStruct<int?>?, double)), "(GenericStruct<int?>?, double)"), //Crazy type
            };

            foreach (var item in tests)
            {
                Assert.Equal(item.Type.ToSimpleCSharpName(), item.Name);
            }            
        }

        private struct GenericStruct<T>
        {
            public T Field;
        }
    }
}

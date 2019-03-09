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
                (typeof(int?), "int?"), //Nullable simple type
                (typeof(TimeSpan), "TimeSpan"), //Type
                (typeof(TimeSpan?), "TimeSpan?"), //Nullable type
                (typeof(int[]), "int[]"), //Array of simple type
                (typeof(int?[]), "int?[]"), //Array of nullable simple type
                (typeof(TimeSpan[]), "TimeSpan[]"), //Array of type
                (typeof(TimeSpan?[]), "TimeSpan?[]"), //Array of nullable type
                (typeof(TimeSpan[,,]), "TimeSpan[,,]"), //Multi-dimesional array of type
                (typeof(Dictionary<string, FactAttribute>), "Dictionary<string, FactAttribute>"), //Generic type
                (typeof((string, FactAttribute)), "(string, FactAttribute)"), //Tuple types
                (typeof((string, FactAttribute)?), "(string, FactAttribute)?"), //Nullable tuple types
                (typeof(Dictionary<string, GenericStruct<int?>>), "Dictionary<string, GenericStruct<int?>>"), //Nested generic type
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

// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Presentation.Extensions;
using Xunit;

namespace Stride.Core.Presentation.Tests
{
    public class TestTypeExtensions
    {
        [Fact]
        public void TestTypeExtensionsToSimpleCSharpNameReturnsCorrectlyFormattedName()
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

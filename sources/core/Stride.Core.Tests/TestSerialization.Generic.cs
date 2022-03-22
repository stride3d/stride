// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xunit;

namespace Stride.Core.Tests
{
    public partial class TestSerialization
    {
        [Theory]
        [InlineData(SerializationBackend.Binary)]
        public void TestMaterializedGenericClass(SerializationBackend serializationBackend)
        {
            var g = new ChildOfGeneric { ValueT = 5 };
            var copy = CopyBySerialization(g, serializationBackend);
            Assert.Equal(g.ValueT, copy.ValueT);
        }

        [Theory(Skip = "AssemblyProcessor has a bug where <T,U> class doesn't include parent serializer.")]
        [InlineData(SerializationBackend.Binary)]
        public void TestMaterializedGenericMultipleClass(SerializationBackend serializationBackend)
        {
            var g = new ChildOfGenericMulti { ValueT = 5, ValueU = "hello" };
            var copy = CopyBySerialization(g, serializationBackend);
            Assert.Equal(g.ValueT, copy.ValueT);
        }
    }

    [DataContract(Inherited = true)]
    internal class GenericTestSubject<T>
    {
        public T ValueT { get; set; }
    }

    internal class ChildOfGeneric : GenericTestSubject<int>
    {
    }

    internal class GenericTestSubjectMulti<T, U> : GenericTestSubject<T>
    {
        public U ValueU { get; set; }
    }

    internal class ChildOfGenericMulti : GenericTestSubjectMulti<int, string>
    {
    }
}

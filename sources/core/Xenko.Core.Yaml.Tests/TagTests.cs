// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using NUnit.Framework;
using Xenko.Core.Annotations;
using Xenko.Core.Yaml.Serialization;

// ReSharper disable once CheckNamespace - we explicitely want a custom namespace for the sake of the tests
namespace Xenko.Core.Yaml.Tests.TestNamespace
{
    // Note: do not move these types! If the namespace must be changed, be sure to update TagTests.Namespace.
    #region Types

    // ReSharper disable UnusedTypeParameter
    public class SimpleType { }

    public class SimpleType2 { }

    public class SimpleType3 { }

    public class SimpleType4 { }

    public struct SimpleStruct { }

    public class NestedTypeContainer { public class NestedType { public class NestedType2 { } } }

    public class GenericNestedTypeContainer<T> { public class NestedType { public class NestedType2 { } } }

    public class GenericNestedTypeContainer2<T1, T2> { public class NestedType<T3, T4> { public class NestedType2 { } } }

    public class GenericType<T> { }

    [DataContract("CustomName")]
    public class DataContractType { }

    [DataContract("CustomName2")]
    public class GenericDataContractType<T> { }

    [DataAlias("OldName")]
    public class AliasType { }

    [DataAlias("PreviousName")]
    [DataContract("NewName")]
    public class AliasType2 { }
    
    // ReSharper restore UnusedTypeParameter
    #endregion Types

    public class TagTests : YamlTest
    {
        private const string AssemblyName = "Xenko.Core.Yaml.Tests";
        private const string Namespace = "Xenko.Core.Yaml.Tests.TestNamespace";

        [Test]
        public void TestNullType()
        {
            TestType(null, "!!null");
        }

        [Test]
        public void TestDefaultType()
        {
            TestType(typeof(int), "!!int");
        }

        [Test]
        public void TestCoreType()
        {
            TestType(typeof(Guid), "!System.Guid,mscorlib");
        }

        [Test]
        public void TestSimpleType()
        {
            TestType(typeof(SimpleType), $"!{Namespace}.SimpleType,{AssemblyName}");
        }

        [Test]
        public void TestNullableType()
        {
            // TODO: we would like to have something like "!!int?"
            TestType(typeof(int?), "!System.Nullable%601[[System.Int32,mscorlib]],mscorlib");
            // TODO: we would like to have something like "!System.Guid?,mscorlib"
            TestType(typeof(Guid?), "!System.Nullable%601[[System.Guid,mscorlib]],mscorlib");
            // TODO: we would like to have something like "!Xenko.Core.Yaml.Tests.TestNamespace.SimpleStruct?,Xenko.Core.Yaml.Tests"
            TestType(typeof(SimpleStruct?), $"!System.Nullable%601[[{Namespace}.SimpleStruct,{AssemblyName}]],mscorlib");
        }

        [Test]
        public void TestNestedType()
        {
            TestType(typeof(NestedTypeContainer.NestedType), $"!{Namespace}.NestedTypeContainer+NestedType,{AssemblyName}");
        }

        [Test]
        public void TestDoubleNestedType()
        {
            TestType(typeof(NestedTypeContainer.NestedType.NestedType2), $"!{Namespace}.NestedTypeContainer+NestedType+NestedType2,{AssemblyName}");
        }

        [Test]
        public void TestGenericType()
        {
            TestType(typeof(GenericType<SimpleType>), $"!{Namespace}.GenericType%601[[{Namespace}.SimpleType,{AssemblyName}]],{AssemblyName}");
            TestType(typeof(GenericType<double>), $"!{Namespace}.GenericType%601[[System.Double,mscorlib]],{AssemblyName}");
        }

        [Test]
        public void TestGenericNestedType()
        {
            TestType(typeof(GenericNestedTypeContainer<SimpleType>.NestedType.NestedType2), $"!{Namespace}.GenericNestedTypeContainer%601+NestedType+NestedType2[[{Namespace}.SimpleType,{AssemblyName}]],{AssemblyName}");
            TestType(typeof(GenericNestedTypeContainer<int>.NestedType.NestedType2), $"!{Namespace}.GenericNestedTypeContainer%601+NestedType+NestedType2[[System.Int32,mscorlib]],{AssemblyName}");
        }

        [Test]
        public void TestGenericDoubleNestedType()
        {
            TestType(typeof(GenericNestedTypeContainer2<SimpleType, SimpleType2>.NestedType<SimpleType3, SimpleType4>.NestedType2), $"!{Namespace}.GenericNestedTypeContainer2%602+NestedType%602+NestedType2[[{Namespace}.SimpleType,{AssemblyName}],[{Namespace}.SimpleType2,{AssemblyName}],[{Namespace}.SimpleType3,{AssemblyName}],[{Namespace}.SimpleType4,{AssemblyName}]],{AssemblyName}");
            TestType(typeof(GenericNestedTypeContainer2<int, string>.NestedType<Guid, DateTime>.NestedType2), $"!{Namespace}.GenericNestedTypeContainer2%602+NestedType%602+NestedType2[[System.Int32,mscorlib],[System.String,mscorlib],[System.Guid,mscorlib],[System.DateTime,mscorlib]],{AssemblyName}");
        }

        [Test]
        public void TestNestedGenericType()
        {
            TestType(typeof(GenericType<NestedTypeContainer.NestedType.NestedType2>), $"!{Namespace}.GenericType%601[[{Namespace}.NestedTypeContainer+NestedType+NestedType2,{AssemblyName}]],{AssemblyName}");
        }

        [Test]
        public void TestGenericNestedGenericType()
        {
            TestType(typeof(GenericType<GenericNestedTypeContainer2<SimpleType, SimpleType2>.NestedType<SimpleType3, SimpleType4>.NestedType2>), $"!{Namespace}.GenericType%601[[{Namespace}.GenericNestedTypeContainer2%602+NestedType%602+NestedType2[[{Namespace}.SimpleType,{AssemblyName}],[{Namespace}.SimpleType2,{AssemblyName}],[{Namespace}.SimpleType3,{AssemblyName}],[{Namespace}.SimpleType4,{AssemblyName}]],{AssemblyName}]],{AssemblyName}");
            TestType(typeof(GenericType<GenericNestedTypeContainer2<int, string>.NestedType<Guid, DateTime>.NestedType2>), $"!{Namespace}.GenericType%601[[{Namespace}.GenericNestedTypeContainer2%602+NestedType%602+NestedType2[[System.Int32,mscorlib],[System.String,mscorlib],[System.Guid,mscorlib],[System.DateTime,mscorlib]],{AssemblyName}]],{AssemblyName}");
        }

        [Test]
        public void TestDataContractType()
        {
            TestType(typeof(DataContractType), "!CustomName");
        }

        [Test]
        public void TestGenericDataContractType()
        {
            // TODO: we would like to have: !CustomName2[[Xenko.Core.Yaml.Tests.TestNamespace.SimpleType,Xenko.Core.Yaml.Tests]]
            TestType(typeof(GenericDataContractType<SimpleType>), $"!{Namespace}.GenericDataContractType%601[[{Namespace}.SimpleType,{AssemblyName}]],{AssemblyName}");
            // TODO: we would like to have: !GenericNestedTypeContainer%601[[CustomName]]
            TestType(typeof(GenericNestedTypeContainer<DataContractType>), $"!{Namespace}.GenericNestedTypeContainer%601[[{Namespace}.DataContractType,{AssemblyName}]],{AssemblyName}");
            // TODO: we would like to have: !CustomName2[[CustomName]]
            TestType(typeof(GenericDataContractType<DataContractType>), $"!{Namespace}.GenericDataContractType%601[[{Namespace}.DataContractType,{AssemblyName}]],{AssemblyName}");
        }

        [Test]
        public void TestAliasContractType()
        {
            TestTypeWithAlias(typeof(AliasType), $"!{Namespace}.AliasType,{AssemblyName}", "!OldName");
            TestTypeWithAlias(typeof(AliasType2), "!NewName", "!PreviousName");
        }

        [Test]
        public void TestDefaultTypeArray()
        {
            // TODO: we would like to have: !!int[]
            TestType(typeof(int[]), "!System.Int32[],mscorlib");
        }

        [Test]
        public void TestCoreTypeArray()
        {
            TestType(typeof(Guid[]), "!System.Guid[],mscorlib");
        }

        [Test]
        public void TestSimpleTypeArray()
        {
            TestType(typeof(SimpleType[]), $"!{Namespace}.SimpleType[],{AssemblyName}");
        }

        [Test]
        public void TestSimpleTypeNestedArray()
        {
            TestType(typeof(SimpleType[][][][][]), $"!{Namespace}.SimpleType[][][][][],{AssemblyName}");
        }

        [Test]
        public void TestNestedTypeArray()
        {
            TestType(typeof(NestedTypeContainer.NestedType[]), $"!{Namespace}.NestedTypeContainer+NestedType[],{AssemblyName}");
        }

        [Test]
        public void TestDoubleNestedTypeArray()
        {
            TestType(typeof(NestedTypeContainer.NestedType.NestedType2[]), $"!{Namespace}.NestedTypeContainer+NestedType+NestedType2[],{AssemblyName}");
        }

        [Test]
        public void TestGenericTypeArray()
        {
            TestType(typeof(GenericType<SimpleType>[]), $"!{Namespace}.GenericType%601[[{Namespace}.SimpleType,{AssemblyName}]][],{AssemblyName}");
            TestType(typeof(GenericType<double>[]), $"!{Namespace}.GenericType%601[[System.Double,mscorlib]][],{AssemblyName}");
            TestType(typeof(GenericType<SimpleType[]>), $"!{Namespace}.GenericType%601[[{Namespace}.SimpleType[],{AssemblyName}]],{AssemblyName}");
            TestType(typeof(GenericType<double[]>), $"!{Namespace}.GenericType%601[[System.Double[],mscorlib]],{AssemblyName}");
            TestType(typeof(GenericType<SimpleType[]>[]), $"!{Namespace}.GenericType%601[[{Namespace}.SimpleType[],{AssemblyName}]][],{AssemblyName}");
            TestType(typeof(GenericType<double[]>[]), $"!{Namespace}.GenericType%601[[System.Double[],mscorlib]][],{AssemblyName}");
        }

        [Test]
        public void TestGenericTypeNestedArray()
        {
            TestType(typeof(GenericType<SimpleType>[][]), $"!{Namespace}.GenericType%601[[{Namespace}.SimpleType,{AssemblyName}]][][],{AssemblyName}");
            TestType(typeof(GenericType<double>[][]), $"!{Namespace}.GenericType%601[[System.Double,mscorlib]][][],{AssemblyName}");
            TestType(typeof(GenericType<SimpleType[][]>), $"!{Namespace}.GenericType%601[[{Namespace}.SimpleType[][],{AssemblyName}]],{AssemblyName}");
            TestType(typeof(GenericType<double[][]>), $"!{Namespace}.GenericType%601[[System.Double[][],mscorlib]],{AssemblyName}");
            TestType(typeof(GenericType<SimpleType[][]>[][]), $"!{Namespace}.GenericType%601[[{Namespace}.SimpleType[][],{AssemblyName}]][][],{AssemblyName}");
            TestType(typeof(GenericType<double[][]>[][]), $"!{Namespace}.GenericType%601[[System.Double[][],mscorlib]][][],{AssemblyName}");
        }

        [Test]
        public void TestGenericNestedTypeArray()
        {
            TestType(typeof(GenericNestedTypeContainer<SimpleType>.NestedType.NestedType2[]), $"!{Namespace}.GenericNestedTypeContainer%601+NestedType+NestedType2[[{Namespace}.SimpleType,{AssemblyName}]][],{AssemblyName}");
            TestType(typeof(GenericNestedTypeContainer<int>.NestedType.NestedType2[]), $"!{Namespace}.GenericNestedTypeContainer%601+NestedType+NestedType2[[System.Int32,mscorlib]][],{AssemblyName}");
            TestType(typeof(GenericNestedTypeContainer<SimpleType[]>.NestedType.NestedType2), $"!{Namespace}.GenericNestedTypeContainer%601+NestedType+NestedType2[[{Namespace}.SimpleType[],{AssemblyName}]],{AssemblyName}");
            TestType(typeof(GenericNestedTypeContainer<int[]>.NestedType.NestedType2), $"!{Namespace}.GenericNestedTypeContainer%601+NestedType+NestedType2[[System.Int32[],mscorlib]],{AssemblyName}");
            TestType(typeof(GenericNestedTypeContainer<SimpleType[]>.NestedType.NestedType2[]), $"!{Namespace}.GenericNestedTypeContainer%601+NestedType+NestedType2[[{Namespace}.SimpleType[],{AssemblyName}]][],{AssemblyName}");
            TestType(typeof(GenericNestedTypeContainer<int[]>.NestedType.NestedType2[]), $"!{Namespace}.GenericNestedTypeContainer%601+NestedType+NestedType2[[System.Int32[],mscorlib]][],{AssemblyName}");
        }

        [Test]
        public void TestGenericDoubleNestedTypeArray()
        {
            TestType(typeof(GenericNestedTypeContainer2<SimpleType, SimpleType2>.NestedType<SimpleType3, SimpleType4>.NestedType2[]), $"!{Namespace}.GenericNestedTypeContainer2%602+NestedType%602+NestedType2[[{Namespace}.SimpleType,{AssemblyName}],[{Namespace}.SimpleType2,{AssemblyName}],[{Namespace}.SimpleType3,{AssemblyName}],[{Namespace}.SimpleType4,{AssemblyName}]][],{AssemblyName}");
            TestType(typeof(GenericNestedTypeContainer2<int, string>.NestedType<Guid, DateTime>.NestedType2[]), $"!{Namespace}.GenericNestedTypeContainer2%602+NestedType%602+NestedType2[[System.Int32,mscorlib],[System.String,mscorlib],[System.Guid,mscorlib],[System.DateTime,mscorlib]][],{AssemblyName}");
            TestType(typeof(GenericNestedTypeContainer2<SimpleType[], SimpleType2[]>.NestedType<SimpleType3[], SimpleType4[]>.NestedType2), $"!{Namespace}.GenericNestedTypeContainer2%602+NestedType%602+NestedType2[[{Namespace}.SimpleType[],{AssemblyName}],[{Namespace}.SimpleType2[],{AssemblyName}],[{Namespace}.SimpleType3[],{AssemblyName}],[{Namespace}.SimpleType4[],{AssemblyName}]],{AssemblyName}");
            TestType(typeof(GenericNestedTypeContainer2<int[], string[]>.NestedType<Guid[], DateTime[]>.NestedType2), $"!{Namespace}.GenericNestedTypeContainer2%602+NestedType%602+NestedType2[[System.Int32[],mscorlib],[System.String[],mscorlib],[System.Guid[],mscorlib],[System.DateTime[],mscorlib]],{AssemblyName}");
            TestType(typeof(GenericNestedTypeContainer2<SimpleType[], SimpleType2[]>.NestedType<SimpleType3[], SimpleType4[]>.NestedType2[]), $"!{Namespace}.GenericNestedTypeContainer2%602+NestedType%602+NestedType2[[{Namespace}.SimpleType[],{AssemblyName}],[{Namespace}.SimpleType2[],{AssemblyName}],[{Namespace}.SimpleType3[],{AssemblyName}],[{Namespace}.SimpleType4[],{AssemblyName}]][],{AssemblyName}");
            TestType(typeof(GenericNestedTypeContainer2<int[], string[]>.NestedType<Guid[], DateTime[]>.NestedType2[]), $"!{Namespace}.GenericNestedTypeContainer2%602+NestedType%602+NestedType2[[System.Int32[],mscorlib],[System.String[],mscorlib],[System.Guid[],mscorlib],[System.DateTime[],mscorlib]][],{AssemblyName}");
        }

        [Test]
        public void TestNestedGenericTypeArray()
        {
            TestType(typeof(GenericType<NestedTypeContainer.NestedType.NestedType2>[]), $"!{Namespace}.GenericType%601[[{Namespace}.NestedTypeContainer+NestedType+NestedType2,{AssemblyName}]][],{AssemblyName}");
            TestType(typeof(GenericType<NestedTypeContainer.NestedType.NestedType2[]>), $"!{Namespace}.GenericType%601[[{Namespace}.NestedTypeContainer+NestedType+NestedType2[],{AssemblyName}]],{AssemblyName}");
            TestType(typeof(GenericType<NestedTypeContainer.NestedType.NestedType2[]>[]), $"!{Namespace}.GenericType%601[[{Namespace}.NestedTypeContainer+NestedType+NestedType2[],{AssemblyName}]][],{AssemblyName}");
        }

        [Test]
        public void TestGenericNestedGenericTypeArray()
        {
            TestType(typeof(GenericType<GenericNestedTypeContainer2<SimpleType, SimpleType2>.NestedType<SimpleType3, SimpleType4>.NestedType2>[]), $"!{Namespace}.GenericType%601[[{Namespace}.GenericNestedTypeContainer2%602+NestedType%602+NestedType2[[{Namespace}.SimpleType,{AssemblyName}],[{Namespace}.SimpleType2,{AssemblyName}],[{Namespace}.SimpleType3,{AssemblyName}],[{Namespace}.SimpleType4,{AssemblyName}]],{AssemblyName}]][],{AssemblyName}");
            TestType(typeof(GenericType<GenericNestedTypeContainer2<int, string>.NestedType<Guid, DateTime>.NestedType2>[]), $"!{Namespace}.GenericType%601[[{Namespace}.GenericNestedTypeContainer2%602+NestedType%602+NestedType2[[System.Int32,mscorlib],[System.String,mscorlib],[System.Guid,mscorlib],[System.DateTime,mscorlib]],{AssemblyName}]][],{AssemblyName}");
            TestType(typeof(GenericType<GenericNestedTypeContainer2<SimpleType, SimpleType2>.NestedType<SimpleType3, SimpleType4>.NestedType2[]>), $"!{Namespace}.GenericType%601[[{Namespace}.GenericNestedTypeContainer2%602+NestedType%602+NestedType2[[{Namespace}.SimpleType,{AssemblyName}],[{Namespace}.SimpleType2,{AssemblyName}],[{Namespace}.SimpleType3,{AssemblyName}],[{Namespace}.SimpleType4,{AssemblyName}]][],{AssemblyName}]],{AssemblyName}");
            TestType(typeof(GenericType<GenericNestedTypeContainer2<int, string>.NestedType<Guid, DateTime>.NestedType2[]>), $"!{Namespace}.GenericType%601[[{Namespace}.GenericNestedTypeContainer2%602+NestedType%602+NestedType2[[System.Int32,mscorlib],[System.String,mscorlib],[System.Guid,mscorlib],[System.DateTime,mscorlib]][],{AssemblyName}]],{AssemblyName}");
            TestType(typeof(GenericType<GenericNestedTypeContainer2<SimpleType[], SimpleType2[]>.NestedType<SimpleType3[], SimpleType4[]>.NestedType2>), $"!{Namespace}.GenericType%601[[{Namespace}.GenericNestedTypeContainer2%602+NestedType%602+NestedType2[[{Namespace}.SimpleType[],{AssemblyName}],[{Namespace}.SimpleType2[],{AssemblyName}],[{Namespace}.SimpleType3[],{AssemblyName}],[{Namespace}.SimpleType4[],{AssemblyName}]],{AssemblyName}]],{AssemblyName}");
            TestType(typeof(GenericType<GenericNestedTypeContainer2<int[], string[]>.NestedType<Guid[], DateTime[]>.NestedType2>), $"!{Namespace}.GenericType%601[[{Namespace}.GenericNestedTypeContainer2%602+NestedType%602+NestedType2[[System.Int32[],mscorlib],[System.String[],mscorlib],[System.Guid[],mscorlib],[System.DateTime[],mscorlib]],{AssemblyName}]],{AssemblyName}");
            TestType(typeof(GenericType<GenericNestedTypeContainer2<SimpleType[], SimpleType2[]>.NestedType<SimpleType3[], SimpleType4[]>.NestedType2[]>[]), $"!{Namespace}.GenericType%601[[{Namespace}.GenericNestedTypeContainer2%602+NestedType%602+NestedType2[[{Namespace}.SimpleType[],{AssemblyName}],[{Namespace}.SimpleType2[],{AssemblyName}],[{Namespace}.SimpleType3[],{AssemblyName}],[{Namespace}.SimpleType4[],{AssemblyName}]][],{AssemblyName}]][],{AssemblyName}");
            TestType(typeof(GenericType<GenericNestedTypeContainer2<int[], string[]>.NestedType<Guid[], DateTime[]>.NestedType2[]>[]), $"!{Namespace}.GenericType%601[[{Namespace}.GenericNestedTypeContainer2%602+NestedType%602+NestedType2[[System.Int32[],mscorlib],[System.String[],mscorlib],[System.Guid[],mscorlib],[System.DateTime[],mscorlib]][],{AssemblyName}]][],{AssemblyName}");
        }

        [Test]
        public void TestGenericNestedGenericTypeDoubleArray()
        {
            TestType(typeof(GenericType<GenericNestedTypeContainer2<SimpleType, SimpleType2>.NestedType<SimpleType3, SimpleType4>.NestedType2>[][]), $"!{Namespace}.GenericType%601[[{Namespace}.GenericNestedTypeContainer2%602+NestedType%602+NestedType2[[{Namespace}.SimpleType,{AssemblyName}],[{Namespace}.SimpleType2,{AssemblyName}],[{Namespace}.SimpleType3,{AssemblyName}],[{Namespace}.SimpleType4,{AssemblyName}]],{AssemblyName}]][][],{AssemblyName}");
            TestType(typeof(GenericType<GenericNestedTypeContainer2<int, string>.NestedType<Guid, DateTime>.NestedType2>[][]), $"!{Namespace}.GenericType%601[[{Namespace}.GenericNestedTypeContainer2%602+NestedType%602+NestedType2[[System.Int32,mscorlib],[System.String,mscorlib],[System.Guid,mscorlib],[System.DateTime,mscorlib]],{AssemblyName}]][][],{AssemblyName}");
            TestType(typeof(GenericType<GenericNestedTypeContainer2<SimpleType, SimpleType2>.NestedType<SimpleType3, SimpleType4>.NestedType2[][]>), $"!{Namespace}.GenericType%601[[{Namespace}.GenericNestedTypeContainer2%602+NestedType%602+NestedType2[[{Namespace}.SimpleType,{AssemblyName}],[{Namespace}.SimpleType2,{AssemblyName}],[{Namespace}.SimpleType3,{AssemblyName}],[{Namespace}.SimpleType4,{AssemblyName}]][][],{AssemblyName}]],{AssemblyName}");
            TestType(typeof(GenericType<GenericNestedTypeContainer2<int, string>.NestedType<Guid, DateTime>.NestedType2[][]>), $"!{Namespace}.GenericType%601[[{Namespace}.GenericNestedTypeContainer2%602+NestedType%602+NestedType2[[System.Int32,mscorlib],[System.String,mscorlib],[System.Guid,mscorlib],[System.DateTime,mscorlib]][][],{AssemblyName}]],{AssemblyName}");
            TestType(typeof(GenericType<GenericNestedTypeContainer2<SimpleType[][], SimpleType2[][]>.NestedType<SimpleType3[][], SimpleType4[][]>.NestedType2>), $"!{Namespace}.GenericType%601[[{Namespace}.GenericNestedTypeContainer2%602+NestedType%602+NestedType2[[{Namespace}.SimpleType[][],{AssemblyName}],[{Namespace}.SimpleType2[][],{AssemblyName}],[{Namespace}.SimpleType3[][],{AssemblyName}],[{Namespace}.SimpleType4[][],{AssemblyName}]],{AssemblyName}]],{AssemblyName}");
            TestType(typeof(GenericType<GenericNestedTypeContainer2<int[][], string[][]>.NestedType<Guid[][], DateTime[][]>.NestedType2>), $"!{Namespace}.GenericType%601[[{Namespace}.GenericNestedTypeContainer2%602+NestedType%602+NestedType2[[System.Int32[][],mscorlib],[System.String[][],mscorlib],[System.Guid[][],mscorlib],[System.DateTime[][],mscorlib]],{AssemblyName}]],{AssemblyName}");
            TestType(typeof(GenericType<GenericNestedTypeContainer2<SimpleType[][], SimpleType2[][]>.NestedType<SimpleType3[][], SimpleType4[][]>.NestedType2[][]>[][]), $"!{Namespace}.GenericType%601[[{Namespace}.GenericNestedTypeContainer2%602+NestedType%602+NestedType2[[{Namespace}.SimpleType[][],{AssemblyName}],[{Namespace}.SimpleType2[][],{AssemblyName}],[{Namespace}.SimpleType3[][],{AssemblyName}],[{Namespace}.SimpleType4[][],{AssemblyName}]][][],{AssemblyName}]][][],{AssemblyName}");
            TestType(typeof(GenericType<GenericNestedTypeContainer2<int[][], string[][]>.NestedType<Guid[][], DateTime[][]>.NestedType2[][]>[][]), $"!{Namespace}.GenericType%601[[{Namespace}.GenericNestedTypeContainer2%602+NestedType%602+NestedType2[[System.Int32[][],mscorlib],[System.String[][],mscorlib],[System.Guid[][],mscorlib],[System.DateTime[][],mscorlib]][][],{AssemblyName}]][][],{AssemblyName}");
        }

        [Test]
        public void TestDataContractTypeArray()
        {
            // TODO: we would like to have: !CustomName[]
            TestType(typeof(DataContractType[]), $"!{Namespace}.DataContractType[],{AssemblyName}");
        }

        [Test]
        public void TestGenericDataContractTypeArray()
        {
            // TODO: we would like to have: !CustomName2[[Xenko.Core.Yaml.Tests.TestNamespace.SimpleType,Xenko.Core.Yaml.Tests]][]
            TestType(typeof(GenericDataContractType<SimpleType>[]), $"!{Namespace}.GenericDataContractType%601[[{Namespace}.SimpleType,{AssemblyName}]][],{AssemblyName}");
            // TODO: we would like to have: !GenericNestedTypeContainer%601[[CustomName]][]
            TestType(typeof(GenericNestedTypeContainer<DataContractType>[]), $"!{Namespace}.GenericNestedTypeContainer%601[[{Namespace}.DataContractType,{AssemblyName}]][],{AssemblyName}");
            // TODO: we would like to have: !GenericNestedTypeContainer%601[[CustomName[]]]
            TestType(typeof(GenericNestedTypeContainer<DataContractType[]>), $"!{Namespace}.GenericNestedTypeContainer%601[[{Namespace}.DataContractType[],{AssemblyName}]],{AssemblyName}");
            // TODO: we would like to have: !CustomName2[[CustomName]][]
            TestType(typeof(GenericDataContractType<DataContractType>[]), $"!{Namespace}.GenericDataContractType%601[[{Namespace}.DataContractType,{AssemblyName}]][],{AssemblyName}");
        }

        [Test,Ignore("Aliases are not supported for arrays")]
        public void TestAliasContractTypeArray()
        {
            // TODO: Support aliases (ie. remapping) for array
            // TODO: Support aliases (ie. remapping) for generic types, too (tests not implemented for this case)
            TestTypeWithAlias(typeof(AliasType[]), $"!{Namespace}.AliasType[],{AssemblyName}", "!OldName[]");
            TestTypeWithAlias(typeof(AliasType2[]), "!NewName", "!PreviousName[]");
        }

        [NotNull]
        private static Serializer CreateSerializer()
        {
            var settings = new SerializerSettings();
            settings.RegisterAssembly(typeof(TagTests).Assembly);
            var serializer = new Serializer(settings);
            settings.AssemblyRegistry.UseShortTypeName = true;
            return serializer;
        }

        private static void TestType(Type type, string tag)
        {
            // NOTE: we're testing twice with different order because each method is caching result and discrepencies between the two methods could cause one of the order to fail
            var serializer = CreateSerializer();
            bool isAlias;
            var retrievedTag = serializer.Settings.TagTypeRegistry.TagFromType(type);
            Assert.AreEqual(tag, retrievedTag);
            var retrivedType = serializer.Settings.TagTypeRegistry.TypeFromTag(tag, out isAlias);
            Assert.False(isAlias);
            Assert.AreEqual(type, retrivedType);

            serializer = CreateSerializer();
            retrivedType = serializer.Settings.TagTypeRegistry.TypeFromTag(tag, out isAlias);
            Assert.False(isAlias);
            Assert.AreEqual(type, retrivedType);
            retrievedTag = serializer.Settings.TagTypeRegistry.TagFromType(type);
            Assert.AreEqual(tag, retrievedTag);
        }

        private static void TestTypeWithAlias(Type type, string tag, string alias)
        {
            // NOTE: we're testing twice with different order because each method is caching result and discrepencies between the two methods could cause one of the order to fail
            var serializer = CreateSerializer();
            bool isAlias;
            var retrievedTag = serializer.Settings.TagTypeRegistry.TagFromType(type);
            Assert.AreEqual(tag, retrievedTag);
            var retrivedType = serializer.Settings.TagTypeRegistry.TypeFromTag(alias, out isAlias);
            Assert.True(isAlias);
            Assert.AreEqual(type, retrivedType);

            serializer = CreateSerializer();
            retrivedType = serializer.Settings.TagTypeRegistry.TypeFromTag(alias, out isAlias);
            Assert.True(isAlias);
            Assert.AreEqual(type, retrivedType);
            retrievedTag = serializer.Settings.TagTypeRegistry.TagFromType(type);
            Assert.AreEqual(tag, retrievedTag);
        }
    }
}

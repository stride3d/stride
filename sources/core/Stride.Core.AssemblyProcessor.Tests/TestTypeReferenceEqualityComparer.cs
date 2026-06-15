// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Mono.Cecil;
using Xunit;

namespace Stride.Core.AssemblyProcessor.Tests;

public class TestTypeReferenceEqualityComparer
{
    private readonly AssemblyDefinition testAssembly;

    public TestTypeReferenceEqualityComparer()
    {
        testAssembly = AssemblyDefinition.ReadAssembly(typeof(TestTypeReferenceEqualityComparer).Assembly.Location);
    }

    [Fact]
    public void TestDefaultInstance()
    {
        Assert.NotNull(TypeReferenceEqualityComparer.Default);
        Assert.Same(TypeReferenceEqualityComparer.Default, TypeReferenceEqualityComparer.Default);
    }

    [Fact]
    public void TestEqualsSameType()
    {
        var intType1 = testAssembly.MainModule.TypeSystem.Int32;
        var intType2 = testAssembly.MainModule.TypeSystem.Int32;

        Assert.True(TypeReferenceEqualityComparer.Default.Equals(intType1, intType2));
    }

    [Fact]
    public void TestEqualsDifferentTypes()
    {
        var intType = testAssembly.MainModule.TypeSystem.Int32;
        var stringType = testAssembly.MainModule.TypeSystem.String;

        Assert.False(TypeReferenceEqualityComparer.Default.Equals(intType, stringType));
    }

    [Fact]
    public void TestEqualsImportedTypes()
    {
        var listType1 = testAssembly.MainModule.ImportReference(typeof(List<int>));
        var listType2 = testAssembly.MainModule.ImportReference(typeof(List<int>));

        Assert.True(TypeReferenceEqualityComparer.Default.Equals(listType1, listType2));
    }

    [Fact]
    public void TestEqualsDifferentGenericArguments()
    {
        var listIntType = testAssembly.MainModule.ImportReference(typeof(List<int>));
        var listStringType = testAssembly.MainModule.ImportReference(typeof(List<string>));

        Assert.False(TypeReferenceEqualityComparer.Default.Equals(listIntType, listStringType));
    }

    [Fact]
    public void TestGetHashCodeConsistency()
    {
        var intType = testAssembly.MainModule.TypeSystem.Int32;

        var hash1 = TypeReferenceEqualityComparer.Default.GetHashCode(intType);
        var hash2 = TypeReferenceEqualityComparer.Default.GetHashCode(intType);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void TestGetHashCodeEqualTypes()
    {
        var intType1 = testAssembly.MainModule.TypeSystem.Int32;
        var intType2 = testAssembly.MainModule.TypeSystem.Int32;

        var hash1 = TypeReferenceEqualityComparer.Default.GetHashCode(intType1);
        var hash2 = TypeReferenceEqualityComparer.Default.GetHashCode(intType2);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void TestGetHashCodeDifferentTypes()
    {
        var intType = testAssembly.MainModule.TypeSystem.Int32;
        var stringType = testAssembly.MainModule.TypeSystem.String;

        var hash1 = TypeReferenceEqualityComparer.Default.GetHashCode(intType);
        var hash2 = TypeReferenceEqualityComparer.Default.GetHashCode(stringType);

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void TestUsageInHashSet()
    {
        var intType = testAssembly.MainModule.TypeSystem.Int32;
        var stringType = testAssembly.MainModule.TypeSystem.String;
        var intType2 = testAssembly.MainModule.TypeSystem.Int32;

        var hashSet = new HashSet<TypeReference>(TypeReferenceEqualityComparer.Default);

        Assert.True(hashSet.Add(intType));
        Assert.True(hashSet.Add(stringType));
        Assert.False(hashSet.Add(intType2)); // Should not add duplicate

        Assert.Equal(2, hashSet.Count);
    }

    [Fact]
    public void TestUsageInDictionary()
    {
        var intType = testAssembly.MainModule.TypeSystem.Int32;
        var stringType = testAssembly.MainModule.TypeSystem.String;

        var dictionary = new Dictionary<TypeReference, string>(TypeReferenceEqualityComparer.Default);

        dictionary[intType] = "Integer";
        dictionary[stringType] = "String";

        Assert.Equal(2, dictionary.Count);
        Assert.Equal("Integer", dictionary[intType]);
        Assert.Equal("String", dictionary[stringType]);
    }
}

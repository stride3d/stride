// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Mono.Cecil;
using Xunit;

namespace Stride.Core.AssemblyProcessor.Tests;

public class TestCecilExtensions
{
    class Nested;
    class GenericNested<T>;

    private readonly BaseAssemblyResolver assemblyResolver = new DefaultAssemblyResolver();
    private readonly AssemblyDefinition testAssembly;

    public TestCecilExtensions()
    {
        // Add location of current assembly to MonoCecil search path.
        var assemblyLocation = Path.GetDirectoryName(typeof(TestCecilExtensions).Assembly.Location);
        assemblyResolver.AddSearchDirectory(assemblyLocation);

        // Load test assembly for Cecil operations
        testAssembly = AssemblyDefinition.ReadAssembly(typeof(TestCecilExtensions).Assembly.Location);
    }

    private string GenerateNameCecil(Type type)
    {
        var typeReference = type.GenerateTypeCecil(assemblyResolver);
        return typeReference.ConvertAssemblyQualifiedName();
    }

    private static string? GenerateNameDotNet(Type type)
    {
        return type.AssemblyQualifiedName;
    }

    private void CheckGeneratedNames(Type type)
    {
        var nameCecil = GenerateNameCecil(type);
        var nameDotNet = GenerateNameDotNet(type);
        Assert.Equal(nameDotNet, nameCecil);
    }

    [Fact]
    public void TestAssemblyQualifiedNamesPrimitiveTypes()
    {
        CheckGeneratedNames(typeof(bool));
        CheckGeneratedNames(typeof(byte));
        CheckGeneratedNames(typeof(sbyte));
        CheckGeneratedNames(typeof(short));
        CheckGeneratedNames(typeof(ushort));
        CheckGeneratedNames(typeof(int));
        CheckGeneratedNames(typeof(uint));
        CheckGeneratedNames(typeof(long));
        CheckGeneratedNames(typeof(ulong));
        CheckGeneratedNames(typeof(float));
        CheckGeneratedNames(typeof(double));
        CheckGeneratedNames(typeof(decimal));
        CheckGeneratedNames(typeof(char));
        CheckGeneratedNames(typeof(string));
    }

    [Fact]
    public void TestAssemblyQualifiedNamesUserTypes()
    {
        CheckGeneratedNames(typeof(TestCecilExtensions));
        CheckGeneratedNames(typeof(Nested));
    }

    [Fact]
    public void TestAssemblyQualifiedNamesGenericTypes()
    {
        // Closed generics
        CheckGeneratedNames(typeof(List<int>));
        CheckGeneratedNames(typeof(Dictionary<string, object>));
        CheckGeneratedNames(typeof(Dictionary<int, List<string>>));

        // Open generics
        CheckGeneratedNames(typeof(List<>));
        CheckGeneratedNames(typeof(Dictionary<,>));
    }

    [Fact]
    public void TestAssemblyQualifiedNamesArrayTypes()
    {
        CheckGeneratedNames(typeof(int[]));
        CheckGeneratedNames(typeof(string[]));
        CheckGeneratedNames(typeof(Dictionary<string, object>[]));
        CheckGeneratedNames(typeof(int[,]));
        CheckGeneratedNames(typeof(int[,,]));
    }

    [Fact]
    public void TestAssemblyQualifiedNamesNullableTypes()
    {
        CheckGeneratedNames(typeof(bool?));
        CheckGeneratedNames(typeof(int?));
        CheckGeneratedNames(typeof(decimal?));
    }

    [Fact]
    public void TestConvertCSharpTypeName()
    {
        var intType = testAssembly.MainModule.TypeSystem.Int32;
        Assert.Equal("System.Int32", intType.ConvertCSharp(false));

        var stringType = testAssembly.MainModule.TypeSystem.String;
        Assert.Equal("System.String", stringType.ConvertCSharp(false));
    }

    [Fact]
    public void TestConvertCSharpGenericTypeName()
    {
        var listType = testAssembly.MainModule.ImportReference(typeof(List<int>));
        var result = listType.ConvertCSharp(false);
        Assert.Contains("System.Collections.Generic.List<System.Int32>", result);
    }

    [Fact]
    public void TestConvertCSharpEmptyGenericTypeName()
    {
        var listType = testAssembly.MainModule.ImportReference(typeof(List<>));
        var result = listType.ConvertCSharp(true);
        Assert.Contains("System.Collections.Generic.List<>", result);
    }

    [Fact]
    public void TestConvertCSharpArrayTypeName()
    {
        var arrayType = testAssembly.MainModule.ImportReference(typeof(int[]));
        var result = arrayType.ConvertCSharp(false);
        Assert.Equal("System.Int32[]", result);
    }

    [Fact]
    public void TestIsResolvedValueType()
    {
        var intType = testAssembly.MainModule.TypeSystem.Int32;
        Assert.True(intType.IsResolvedValueType());

        var stringType = testAssembly.MainModule.TypeSystem.String;
        Assert.False(stringType.IsResolvedValueType());
    }

    [Fact]
    public void TestMakeGenericType()
    {
        var listTypeDef = testAssembly.MainModule.ImportReference(typeof(List<>)).Resolve();
        var intType = testAssembly.MainModule.TypeSystem.Int32;

        var genericInstance = listTypeDef.MakeGenericType(intType);

        Assert.IsType<GenericInstanceType>(genericInstance);
        var git = (GenericInstanceType)genericInstance;
        Assert.Single(git.GenericArguments);
        Assert.Equal(intType.FullName, git.GenericArguments[0].FullName);
    }

    [Fact]
    public void TestMakeGenericTypeInvalidArgumentCount()
    {
        var listTypeDef = testAssembly.MainModule.ImportReference(typeof(List<>)).Resolve();
        var intType = testAssembly.MainModule.TypeSystem.Int32;
        var stringType = testAssembly.MainModule.TypeSystem.String;

        // List<T> expects 1 type argument, not 2
        Assert.Throws<ArgumentException>(() => listTypeDef.MakeGenericType(intType, stringType));
    }

    [Fact]
    public void TestMakeGenericTypeNoArguments()
    {
        var listTypeDef = testAssembly.MainModule.ImportReference(typeof(List<>)).Resolve();

        // List<T> expects 1 type argument, providing 0 should throw
        Assert.Throws<ArgumentException>(() => listTypeDef.MakeGenericType());
    }
}

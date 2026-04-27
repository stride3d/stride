// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;

namespace Stride.Core.AssemblyProcessor.Tests;

public class TestCecilExtensionsAdvanced
{
    private readonly AssemblyDefinition testAssembly;
    private readonly BaseAssemblyResolver assemblyResolver;

    public TestCecilExtensionsAdvanced()
    {
        assemblyResolver = new DefaultAssemblyResolver();
        var assemblyLocation = Path.GetDirectoryName(typeof(TestCecilExtensionsAdvanced).Assembly.Location);
        assemblyResolver.AddSearchDirectory(assemblyLocation);
        testAssembly = AssemblyDefinition.ReadAssembly(typeof(TestCecilExtensionsAdvanced).Assembly.Location);
    }

    [Fact]
    public void TestGetEmptyConstructorPublic()
    {
        var listType = testAssembly.MainModule.ImportReference(typeof(List<int>)).Resolve();
        var constructor = listType.GetEmptyConstructor();

        Assert.NotNull(constructor);
        Assert.True(constructor.IsConstructor);
        Assert.Empty(constructor.Parameters);
        Assert.True(constructor.IsPublic);
    }

    [Fact]
    public void TestGetEmptyConstructorPrivate()
    {
        // Create a type definition with only a private constructor for testing
        var testType = new TypeDefinition("Test", "PrivateConstructorTest",
            Mono.Cecil.TypeAttributes.Class | Mono.Cecil.TypeAttributes.Public);

        var privateCtor = new MethodDefinition(".ctor",
            Mono.Cecil.MethodAttributes.Private | Mono.Cecil.MethodAttributes.HideBySig |
            Mono.Cecil.MethodAttributes.SpecialName | Mono.Cecil.MethodAttributes.RTSpecialName,
            testAssembly.MainModule.TypeSystem.Void);
        testType.Methods.Add(privateCtor);

        // Should not find without allowPrivate
        Assert.Null(testType.GetEmptyConstructor(false));

        // Should find with allowPrivate
        Assert.NotNull(testType.GetEmptyConstructor(true));
    }

    [Fact]
    public void TestGetEmptyConstructorNoConstructor()
    {
        var testType = new TypeDefinition("Test", "NoConstructorTest",
            Mono.Cecil.TypeAttributes.Class | Mono.Cecil.TypeAttributes.Public);

        Assert.Null(testType.GetEmptyConstructor());
    }

    [Fact]
    public void TestMakeGenericMethod()
    {
        // Get a generic method reference
        var listType = testAssembly.MainModule.ImportReference(typeof(List<>));
        var toArrayMethod = listType.Resolve().Methods.First(m => m.Name == "ToArray");

        if (toArrayMethod.HasGenericParameters)
        {
            var intType = testAssembly.MainModule.TypeSystem.Int32;
            var genericMethod = toArrayMethod.MakeGenericMethod(intType);

            Assert.IsType<GenericInstanceMethod>(genericMethod);
            var gim = (GenericInstanceMethod)genericMethod;
            Assert.Single(gim.GenericArguments);
        }
    }

    [Fact]
    public void TestMakeGenericMethodInvalidArgumentCount()
    {
        var listType = testAssembly.MainModule.ImportReference(typeof(List<>));
        var toArrayMethod = listType.Resolve().Methods.First(m => m.Name == "ToArray");

        if (toArrayMethod.HasGenericParameters && toArrayMethod.GenericParameters.Count == 1)
        {
            var intType = testAssembly.MainModule.TypeSystem.Int32;
            var stringType = testAssembly.MainModule.TypeSystem.String;

            // Providing wrong number of type arguments
            Assert.Throws<ArgumentException>(() => toArrayMethod.MakeGenericMethod(intType, stringType));
        }
    }

    [Fact]
    public void TestMakeGenericField()
    {
        var dictType = testAssembly.MainModule.ImportReference(typeof(Dictionary<,>)).Resolve();
        var field = dictType.Fields.FirstOrDefault();

        if (field != null)
        {
            var intType = testAssembly.MainModule.TypeSystem.Int32;
            var stringType = testAssembly.MainModule.TypeSystem.String;

            var genericField = field.MakeGeneric(intType, stringType);

            Assert.NotNull(genericField);
            Assert.IsType<GenericInstanceType>(genericField.DeclaringType);
        }
    }

    [Fact]
    public void TestMakeGenericNoArguments()
    {
        var listType = testAssembly.MainModule.ImportReference(typeof(List<>)).Resolve();
        var addMethod = listType.Methods.First(m => m.Name == "Add");

        var result = addMethod.MakeGeneric();

        // Should return the same reference when no arguments provided
        Assert.Same(addMethod, result);
    }

    [Fact]
    public void TestOpenModuleConstructor()
    {
        var assembly = AssemblyDefinition.CreateAssembly(
            new AssemblyNameDefinition("TestAssembly", new Version(1, 0, 0, 0)),
            "TestModule",
            ModuleKind.Dll);

        var staticCtor = assembly.OpenModuleConstructor(out var returnInstruction);

        Assert.NotNull(staticCtor);
        Assert.True(staticCtor.IsStatic);
        Assert.True(staticCtor.IsConstructor);
        Assert.NotNull(returnInstruction);
        Assert.Equal(OpCodes.Ret, returnInstruction.OpCode);
    }

    [Fact]
    public void TestOpenModuleConstructorExisting()
    {
        var assembly = AssemblyDefinition.CreateAssembly(
            new AssemblyNameDefinition("TestAssembly", new Version(1, 0, 0, 0)),
            "TestModule",
            ModuleKind.Dll);

        // Call twice to ensure it returns existing constructor
        var staticCtor1 = assembly.OpenModuleConstructor(out var returnInstruction1);
        var staticCtor2 = assembly.OpenModuleConstructor(out var returnInstruction2);

        Assert.Same(staticCtor1, staticCtor2);
        Assert.Same(returnInstruction1, returnInstruction2);
    }

    [Fact]
    public void TestFindCorlibAssembly()
    {
        var corlibAssembly = CecilExtensions.FindCorlibAssembly(testAssembly);

        Assert.NotNull(corlibAssembly);
        // Should be either mscorlib or System.Runtime depending on target framework
        Assert.True(
            corlibAssembly.Name.Name.Equals("mscorlib", StringComparison.OrdinalIgnoreCase) ||
            corlibAssembly.Name.Name.Equals("System.Runtime", StringComparison.OrdinalIgnoreCase),
            $"Expected mscorlib or System.Runtime, got {corlibAssembly.Name.Name}");
    }

    [Fact]
    public void TestFindCollectionsAssembly()
    {
        var collectionsAssembly = CecilExtensions.FindCollectionsAssembly(testAssembly);

        Assert.NotNull(collectionsAssembly);
        // Should contain collection types
        Assert.True(
            collectionsAssembly.Name.Name.Equals("mscorlib", StringComparison.OrdinalIgnoreCase) ||
            collectionsAssembly.Name.Name.Equals("System.Collections", StringComparison.OrdinalIgnoreCase),
            $"Expected mscorlib or System.Collections, got {collectionsAssembly.Name.Name}");
    }

    [Fact]
    public void TestFindReflectionAssembly()
    {
        var reflectionAssembly = CecilExtensions.FindReflectionAssembly(testAssembly);

        Assert.NotNull(reflectionAssembly);
        // Should contain reflection types
        Assert.True(
            reflectionAssembly.Name.Name.Equals("mscorlib", StringComparison.OrdinalIgnoreCase) ||
            reflectionAssembly.Name.Name.Equals("System.Reflection", StringComparison.OrdinalIgnoreCase),
            $"Expected mscorlib or System.Reflection, got {reflectionAssembly.Name.Name}");
    }

    [Fact]
    public void TestGetTypeResolved()
    {
        // Use a type that exists in the test assembly's module
        var testType = testAssembly.MainModule.GetTypeResolved(typeof(TestCecilExtensionsAdvanced).FullName);

        Assert.NotNull(testType);
        Assert.Equal(typeof(TestCecilExtensionsAdvanced).FullName, testType.FullName);
    }

    [Fact]
    public void TestGetTypeResolvedWithNamespace()
    {
        var testType = testAssembly.MainModule.GetTypeResolved(
            typeof(TestCecilExtensionsAdvanced).Namespace,
            typeof(TestCecilExtensionsAdvanced).Name);

        Assert.NotNull(testType);
        Assert.Equal(typeof(TestCecilExtensionsAdvanced).Namespace, testType.Namespace);
        Assert.Equal(typeof(TestCecilExtensionsAdvanced).Name, testType.Name);
    }

    [Fact]
    public void TestGenerateGenericsOpen()
    {
        var listType = testAssembly.MainModule.ImportReference(typeof(List<>));
        var generics = listType.GenerateGenerics(true);

        Assert.Equal("<>", generics);
    }

    [Fact]
    public void TestGenerateGenericsClosed()
    {
        var listType = testAssembly.MainModule.ImportReference(typeof(List<int>));
        var generics = listType.GenerateGenerics(false);

        Assert.Contains("<", generics);
        Assert.Contains(">", generics);
        Assert.Contains("Int32", generics);
    }

    [Fact]
    public void TestGenerateGenericsNonGeneric()
    {
        var intType = testAssembly.MainModule.TypeSystem.Int32;
        var generics = intType.GenerateGenerics();

        Assert.Equal(string.Empty, generics);
    }

    [Fact]
    public void TestChangeGenericInstanceType()
    {
        var listIntType = (GenericInstanceType)testAssembly.MainModule.ImportReference(typeof(List<int>));
        var stringType = testAssembly.MainModule.TypeSystem.String;

        var newGenericArgs = new[] { stringType };
        var result = listIntType.ChangeGenericInstanceType(listIntType.ElementType, newGenericArgs);

        Assert.IsType<GenericInstanceType>(result);
        Assert.Single(result.GenericArguments);
        Assert.Equal(stringType.FullName, result.GenericArguments[0].FullName);
    }

    [Fact]
    public void TestChangeGenericInstanceTypeSameArguments()
    {
        var listIntType = (GenericInstanceType)testAssembly.MainModule.ImportReference(typeof(List<int>));

        var result = listIntType.ChangeGenericInstanceType(listIntType.ElementType, listIntType.GenericArguments);

        // Should return same instance when nothing changed
        Assert.Same(listIntType, result);
    }

    [Fact]
    public void TestChangeArrayType()
    {
        var intArrayType = new ArrayType(testAssembly.MainModule.TypeSystem.Int32, 1);
        var stringType = testAssembly.MainModule.TypeSystem.String;

        var result = intArrayType.ChangeArrayType(stringType, 1);

        Assert.Equal(stringType.FullName, result.ElementType.FullName);
        Assert.Equal(1, result.Rank);
    }

    [Fact]
    public void TestChangeArrayTypeRank()
    {
        var intArrayType = new ArrayType(testAssembly.MainModule.TypeSystem.Int32, 1);

        var result = intArrayType.ChangeArrayType(testAssembly.MainModule.TypeSystem.Int32, 2);

        Assert.Equal(2, result.Rank);
    }

    [Fact]
    public void TestChangeArrayTypeSame()
    {
        var intArrayType = new ArrayType(testAssembly.MainModule.TypeSystem.Int32, 1);

        var result = intArrayType.ChangeArrayType(testAssembly.MainModule.TypeSystem.Int32, 1);

        // Should return same instance when nothing changed
        Assert.Same(intArrayType, result);
    }

    [Fact]
    public void TestAddRangeToList()
    {
        var list = new List<int> { 1, 2, 3 };
        var itemsToAdd = new[] { 4, 5, 6 };

        list.AddRange(itemsToAdd);

        Assert.Equal(6, list.Count);
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6 }, list);
    }

    [Fact]
    public void TestAddRangeToCollection()
    {
        ICollection<int> collection = new List<int> { 1, 2, 3 };
        var itemsToAdd = new[] { 4, 5, 6 };

        collection.AddRange(itemsToAdd);

        Assert.Equal(6, collection.Count);
    }

    [Fact]
    public void TestContainsGenericParameter()
    {
        var listTypeDef = testAssembly.MainModule.ImportReference(typeof(List<>)).Resolve();

        // Generic type definition has generic parameters but they are defined (not unresolved)
        // The method checks if there are unresolved generic parameters in the type hierarchy
        // Let's test with a generic instance that has a generic parameter
        var genericParam = new GenericParameter("T", listTypeDef);
        var genericInstance = new GenericInstanceType(listTypeDef);
        genericInstance.GenericArguments.Add(genericParam);

        Assert.True(genericInstance.ContainsGenericParameter());
    }
}

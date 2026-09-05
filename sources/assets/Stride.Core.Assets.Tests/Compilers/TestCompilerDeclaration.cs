// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Compiler;
using Stride.Core.Assets.Tests.Compilers;

[assembly: DeclaredCompiler(typeof(DeclaredCompilerAsset), "declared")]

namespace Stride.Core.Assets.Tests.Compilers;

/// <summary>
/// An asset-level context distinct from <see cref="AssetCompilationContext"/> so the declared compiler
/// cannot be confused with the type-attributed ones of the other tests.
/// </summary>
public class DeclaredCompilationContext : ICompilationContext
{
}

[DataContract]
public class DeclaredCompilerAsset : Asset
{
}

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class DeclaredCompilerAttribute : Attribute, IAssetCompilerDeclaration
{
    public DeclaredCompilerAttribute(Type assetType, string name)
    {
        AssetType = assetType;
        Name = name;
    }

    public Type AssetType { get; }

    public string Name { get; }

    public Type CompilationContext => typeof(DeclaredCompilationContext);

    public IAssetCompiler CreateCompiler(Assembly assembly) => new DeclaredCompiler(Name, assembly);
}

public sealed class DeclaredCompiler : TestCompilerBase
{
    public DeclaredCompiler(string name, Assembly assembly)
    {
        Name = name;
        Assembly = assembly;
    }

    public string Name { get; }

    public Assembly Assembly { get; }

    public override AssetCompilerResult Prepare(AssetCompilerContext context, AssetItem assetItem) => new(Name);
}

public class TestCompilerDeclaration
{
    [Fact]
    public void AssemblyDeclarationRegistersCompiler()
    {
        var compiler = BuildDependencyManager.AssetCompilerRegistry.GetCompiler(typeof(DeclaredCompilerAsset), typeof(DeclaredCompilationContext));

        var declared = Assert.IsType<DeclaredCompiler>(compiler);
        Assert.Equal("declared", declared.Name);
        Assert.Same(typeof(TestCompilerDeclaration).Assembly, declared.Assembly);
    }
}

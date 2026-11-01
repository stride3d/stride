// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Serialization;
using Stride.Core.Storage;
using Stride.Rendering;
using Stride.Shaders.Compiler;

namespace Stride.Shaders.Parsers.Tests;

public class EffectIdTests
{
    private static ShaderMixinSource Tree(int classCount, string lastClass = "Last")
    {
        var tree = new ShaderMixinSource { Name = "Effect" };
        for (var i = 0; i < classCount; i++)
            tree.Mixins.Add(new ShaderClassSource($"Stride.Rendering.Materials.ComputeColors.ComputeColor{i}"));
        tree.Mixins.Add(new ShaderClassSource(lastClass));
        return tree;
    }

    // UTF-16 names, so 5000 classes are far more than 64 KB
    [Fact]
    public void ALargeTreeHasAnId()
    {
        var parameters = EffectCompilerParameters.Default;

        Assert.Equal(ShaderMixinObjectId.Compute(Tree(5000), parameters), ShaderMixinObjectId.Compute(Tree(5000), parameters));
        Assert.NotEqual(ShaderMixinObjectId.Compute(Tree(5000), parameters), ShaderMixinObjectId.Compute(Tree(5000, "Other"), parameters));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5000)]
    public void TheIdIsTheHashOfTheWholeTree(int classCount)
    {
        var parameters = EffectCompilerParameters.Default;
        var tree = Tree(classCount);

        using var bytes = new MemoryStream();
        var writer = new HashSerializationWriter(bytes);
        writer.Context.SerializerSelector = new SerializerSelector("Default", "Hash");
        writer.Write(EffectBytecode.MagicHeader);
        writer.Write(ShaderCompilerVersion.Shader);
        writer.Write(ShaderCompilerVersion.Effect);
        writer.Write(tree);
        writer.Write(parameters.Platform);
        writer.Write(parameters.Profile);
        writer.Write(parameters.Debug);
        writer.Write(parameters.OptimizationLevel);

        var builder = new ObjectIdBuilder();
        builder.Write(new ReadOnlySpan<byte>(bytes.GetBuffer(), 0, (int)bytes.Length));

        Assert.Equal(builder.ComputeHash(), ShaderMixinObjectId.Compute(tree, parameters));
    }
}

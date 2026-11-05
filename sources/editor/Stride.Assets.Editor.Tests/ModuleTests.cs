// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation;
using Stride.Core.Reflection;
using Xunit;

namespace Stride.Assets.Editor.Tests;

public sealed class ModuleTests
{
    [Fact]
    public void LoadingTheAssemblyRegistersItAndTheLayersBelowAsAssetsAssemblies()
    {
        // The assembly's one module (it also holds the former shared assets editor layer) registers the editor layer and the
        // asset view models, then itself
        var assets = AssemblyRegistry.Find(AssemblyCommonCategories.Assets);

        Assert.Contains(typeof(Stride.Editor.Thumbnails.ThumbnailCompilerBase<>).Assembly, assets);
        Assert.Contains(typeof(StrideDefaultAssetsPlugin).Assembly, assets);
        Assert.Contains(typeof(StrideEditorPlugin).Assembly, assets);
    }
}

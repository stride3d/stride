// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using Stride.Assets.Models;
using Stride.Assets.Presentation.AssetEditors.Gizmos;
using Stride.Assets.Presentation.Thumbnails;
using Stride.Core.Assets.Compiler;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Reflection;
using Stride.Editor.Thumbnails;
using Stride.Engine.Gizmos;

namespace Stride.Assets.Presentation.Common.Tests;

/// <summary>
/// The editor finds gizmos and thumbnail compilers by scanning the registered assets assemblies: loading the shared
/// Assets.Presentation assembly must register the whole shared stack.
/// </summary>
public class TestSharedDiscovery
{
    public TestSharedDiscovery()
    {
        RuntimeHelpers.RunModuleConstructor(typeof(GizmoBase).Module.ModuleHandle);
    }

    [Fact]
    public void SharedAssembliesAreRegisteredAsAssetsAssemblies()
    {
        var assemblies = AssemblyRegistry.Find(AssemblyCommonCategories.Assets);
        Assert.Contains(typeof(GizmoBase).Assembly, assemblies);
        Assert.Contains(typeof(ThumbnailCompilerBase<>).Assembly, assemblies);
        Assert.Contains(typeof(DisplayData).Assembly, assemblies);
    }

    [Theory]
    [InlineData(typeof(CameraGizmo))]
    [InlineData(typeof(DispatcherLightGizmo))]
    [InlineData(typeof(ModelGizmo))]
    [InlineData(typeof(ScriptGizmo))]
    public void GizmosAreInTheScanIndex(Type gizmo)
    {
        Assert.Contains(gizmo, AssemblyRegistry.GetScanTypes(typeof(GizmoBase).Assembly, typeof(GizmoComponentAttribute)));
    }

    [Fact]
    public void ThumbnailCompilersAreRegistered()
    {
        var registry = new AssetCompilerRegistry();
        Assert.IsType<ModelThumbnailCompiler>(registry.GetCompiler(typeof(ModelAsset), typeof(ThumbnailCompilationContext)));
    }
}

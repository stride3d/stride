// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Common.Tests.Helpers;
using Stride.Core.Diagnostics;
using Stride.Core.Reflection;

namespace Stride.Core.Assets.Editor.Common.Tests;

[CollectionDefinition(nameof(AssemblyRegistryCollection), DisableParallelization = true)]
public sealed class AssemblyRegistryCollection;

/// <summary>
/// The editor shows the images that a registered assembly without a plugin declares (the runtime or Assets assembly of a
/// package), as long as the assembly is registered.
/// </summary>
[Collection(nameof(AssemblyRegistryCollection))]
public sealed class TestDeclaredImagesOfAssemblies
{
    private const string Category = "DeclaredImagesTests";

    [Fact]
    public void TheHostShowsTheImagesOfTheRegisteredAssembliesWhileTheyAreRegistered()
    {
        // The test assembly declares images; the host has no plugin of it
        var assembly = typeof(PluginHostDeclaredType).Assembly;
        var categories = AssemblyRegistry.GetCategories(assembly);
        var host = new RecordingEditorPluginHost([]) { ShowsAssemblyImages = true };
        var session = new FakeSession();
        try
        {
            AssemblyRegistry.Register(assembly, Category);
            host.RegisterSession(session, new LoggerResult());

            Assert.IsType<byte[]>(host.GetImageForType(typeof(PluginHostDeclaredType)));
            Assert.IsType<byte[]>(host.GetImageForEnum(session, PluginHostImageEnum.Declared));

            AssemblyRegistry.Unregister(assembly);
            Assert.Null(host.GetImageForType(typeof(PluginHostDeclaredType)));
            Assert.False(host.HasImagesForEnum(session, typeof(PluginHostImageEnum)));

            // An assembly registered after the session opened
            AssemblyRegistry.Register(assembly, Category);
            Assert.IsType<byte[]>(host.GetImageForType(typeof(PluginHostDeclaredType)));

            host.Dispose();
            Assert.Null(host.GetImageForType(typeof(PluginHostDeclaredType)));

            // A disposed host no longer follows the registry
            AssemblyRegistry.Unregister(assembly);
            AssemblyRegistry.Register(assembly, Category);
            Assert.Null(host.GetImageForType(typeof(PluginHostDeclaredType)));
        }
        finally
        {
            host.Dispose();
            AssemblyRegistry.Unregister(assembly);
            if (categories.Count > 0)
                AssemblyRegistry.Register(assembly, categories);
        }
    }

    [Fact]
    public void AHostOfGivenPluginsShowsOnlyTheImagesOfThePluginAssemblies()
    {
        var assembly = typeof(PluginHostDeclaredType).Assembly;
        var categories = AssemblyRegistry.GetCategories(assembly);
        var host = new RecordingEditorPluginHost([]);
        try
        {
            AssemblyRegistry.Register(assembly, Category);
            host.RegisterSession(new FakeSession(), new LoggerResult());

            Assert.Null(host.GetImageForType(typeof(PluginHostDeclaredType)));
        }
        finally
        {
            host.Dispose();
            AssemblyRegistry.Unregister(assembly);
            if (categories.Count > 0)
                AssemblyRegistry.Register(assembly, categories);
        }
    }
}

// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Diagnostics;
using Stride.Core.Reflection;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// A declared image that the editor can't decode (here a Git LFS pointer instead of the image file) is left out alone: it is
/// reported as an error of the plugin, or of the assembly when no plugin of the session has it, and the session still opens
/// with the other images.
/// </summary>
[Collection(nameof(AssemblyRegistryCollection))]
public sealed class TestDeclaredImageFailures
{
    private const string Category = "DeclaredImageFailuresTests";
    private const string BrokenResource = "TestBrokenImage.txt";

    [Fact]
    public void AnImageOfARegisteredAssemblyThatCantBeDecodedIsReportedOnTheAssembly()
    {
        // The test assembly declares a good image and a broken one; the host has no plugin of it
        var assembly = typeof(PluginHostBrokenImageType).Assembly;
        var assemblyName = assembly.GetName().Name!;
        var categories = AssemblyRegistry.GetCategories(assembly);
        var host = new RecordingEditorPluginHost([]) { ShowsAssemblyImages = true, ImageDecoder = DecodeLikeABitmap };
        var changes = new List<PluginStatusChangedEventArgs>();
        host.PluginStatusChanged += (_, e) => changes.Add(e);
        var log = new LoggerResult();
        try
        {
            AssemblyRegistry.Register(assembly, Category);
            host.RegisterSession(new FakeSession(), log);

            // The session opened with the good image; the broken one is left out
            Assert.IsType<byte[]>(host.GetImageForType(typeof(PluginHostDeclaredType)));
            Assert.Null(host.GetImageForType(typeof(PluginHostBrokenImageType)));

            var status = Assert.Single(host.PluginStatuses);
            Assert.Null(status.Plugin);
            Assert.Equal(assemblyName, status.Name);
            Assert.Equal(PluginState.Joined, status.State);
            var error = Assert.Single(status.Errors);
            Assert.Equal(PluginContributions.Image(BrokenResource), error.Contribution);
            Assert.IsType<ArgumentException>(error.Exception);
            Assert.Equal([PluginContributions.Image(BrokenResource)], status.TurnedOffContributions);
            Assert.Contains(changes, x => x.Name == assemblyName && x.Status is not null);
            Assert.Contains(log.Messages, x => x.Type == LogMessageType.Warning && x.Text.Contains(BrokenResource) && x.Text.Contains(assemblyName));

            // The Plugins panel lists it by its name, with the error
            var info = Assert.Single(PluginCatalog.Collect(host, null), x => x.Id == assemblyName);
            Assert.Equal(PluginState.Joined, info.State);
            Assert.False(info.CanSwitchOff);
            Assert.Single(info.Statuses.SelectMany(x => x.Errors));

            // The status goes with the assembly
            changes.Clear();
            AssemblyRegistry.Unregister(assembly);
            Assert.Empty(host.PluginStatuses);
            Assert.Contains(changes, x => x.Name == assemblyName && x.Status is null);
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
    public void AnImageOfAJoiningPluginThatCantBeDecodedIsReportedOnThePlugin()
    {
        // The plugin's assembly (the test assembly) declares the good and the broken image
        var plugin = new TestEditorPluginHost.LoggingPlugin("images");
        var host = new RecordingEditorPluginHost([plugin]) { ImageDecoder = DecodeLikeABitmap };
        var log = new LoggerResult();
        try
        {
            host.RegisterSession(new FakeSession(), log);

            // The plugin joined, with the good image, and the broken one is an error of the plugin, turned off alone
            Assert.Contains(plugin, host.SessionPlugins);
            Assert.IsType<byte[]>(host.GetImageForType(typeof(PluginHostDeclaredType)));
            Assert.Null(host.GetImageForType(typeof(PluginHostBrokenImageType)));
            var status = Assert.Single(host.PluginStatuses);
            Assert.Same(plugin, status.Plugin);
            Assert.Equal(PluginState.Joined, status.State);
            var error = Assert.Single(status.Errors);
            Assert.Equal(PluginContributions.Image(BrokenResource), error.Contribution);
            Assert.Equal([PluginContributions.Image(BrokenResource)], status.TurnedOffContributions);
            Assert.Contains(log.Messages, x => x.Type == LogMessageType.Warning && x.Text.Contains(BrokenResource) && x.Text.Contains(plugin.GetType().FullName!));
        }
        finally
        {
            host.Dispose();
        }
    }

    // As a toolkit's bitmap decoder does with a file that is not an image: the LFS pointer text is refused
    private static object DecodeLikeABitmap(byte[] data)
    {
        if (Encoding.ASCII.GetString(data).StartsWith("version https://git-lfs", StringComparison.Ordinal))
            throw new ArgumentException("Unable to load bitmap from provided data");
        return data;
    }
}

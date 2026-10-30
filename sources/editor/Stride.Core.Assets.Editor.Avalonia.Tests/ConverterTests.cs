// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Avalonia;
using Stride.Core.Assets.Editor.Avalonia.Converters;
using Stride.Core.Assets.Editor.Avalonia.Tests.Helpers;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.Tests;

public sealed class ConverterTests
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    [Fact]
    public void DifferentValuesConvertersReplaceTheMarker()
    {
        var marker = NodeViewModel.DifferentValues;

        Assert.Null(new DifferentValuesToNull().Convert(marker, typeof(object), null, Culture));
        Assert.Equal(3, new DifferentValuesToNull().Convert(3, typeof(object), null, Culture));
        Assert.Equal("fallback", new DifferentValuesToParam().Convert(marker, typeof(object), "fallback", Culture));
        Assert.Equal(3, new DifferentValuesToParam().Convert(3, typeof(object), "fallback", Culture));
        Assert.Equal(3, new DifferentValuesToParam().ConvertBack(3, typeof(object), "fallback", Culture));
        // The text box shows its watermark for different values
        Assert.Null(new DifferentValuesToString().Convert(marker, typeof(string), null, Culture));
        Assert.Equal("3", new DifferentValuesToString().Convert(3, typeof(string), null, Culture));
        Assert.Equal(string.Empty, new DifferentValuesToString().Convert(null, typeof(string), null, Culture));
    }

    [Theory]
    [InlineData("Ground", "Ground")]
    [InlineData("NavigationMesh", "Navigation\u200BMesh")]
    [InlineData("Texture2D", "Texture\u200B2D")]
    [InlineData("LOD0", "LOD\u200B0")]
    [InlineData("my_asset", "my_\u200Basset")]
    public void NameBreakingLineLetsTheTilesWrapNamesBetweenWords(string name, string expected)
    {
        // As the WPF asset tiles: a zero-width space after a symbol and between the words of a camel-cased name
        Assert.Equal(expected, new NameBreakingLine().Convert(name, typeof(string), null, Culture));
        Assert.Null(new NameBreakingLine().Convert(null, typeof(string), null, Culture));
    }

    [Fact]
    public void NodePathToObjectWalksTheNodeHierarchy()
    {
        var context = new NodeViewModelContext();
        var node = context.MemberOf(new SampleObject(), nameof(SampleObject.Integer));
        var converter = new NodePathToObject();

        Assert.Equal("Integer", converter.Convert(node, typeof(object), "Name", Culture));
        Assert.Equal(3, converter.Convert(node, typeof(object), "NodeValue", Culture));
        Assert.Same(node.Parent, converter.Convert(node, typeof(object), "Parent", Culture));
        Assert.Same(node, converter.Convert(node.Parent, typeof(object), "[Integer]", Culture));
        Assert.Same(AvaloniaProperty.UnsetValue, converter.Convert(node, typeof(object), "Parent.Parent.Name", Culture));
        Assert.Same(AvaloniaProperty.UnsetValue, converter.Convert("not a node", typeof(object), "Name", Culture));
        Assert.Same(AvaloniaProperty.UnsetValue, converter.Convert(node, typeof(object), null, Culture));
    }

    [Fact]
    public void TypeToResourceLooksUpThePluginImagesAlongTheBaseTypes()
    {
        TestImagesPlugin.Register(typeof(Base), "base-image");

        Assert.Equal("base-image", TypeToResource.FetchResourceFromType(typeof(Base), true));
        Assert.Equal("base-image", TypeToResource.FetchResourceFromType(typeof(Derived), true));
        Assert.Null(TypeToResource.FetchResourceFromType(typeof(Derived), false));
        Assert.Null(TypeToResource.FetchResourceFromType(typeof(string), true));
        Assert.Null(TypeToResource.FetchResourceFromType(null, true));
        Assert.Equal("base-image", new TypeToResource().Convert(typeof(Derived), typeof(object), null, Culture));
    }

    [Fact]
    public void TypeToResourceAsksThePluginHostAlongTheBaseTypes()
    {
        // The images that the plugins of the session register or the registered assemblies declare
        var pluginService = new ImagesPluginService(new() { [typeof(HostBase)] = "host-image" });

        Assert.Equal("host-image", TypeToResource.FetchResourceFromType(typeof(HostBase), true, pluginService));
        Assert.Equal("host-image", TypeToResource.FetchResourceFromType(typeof(HostDerived), true, pluginService));
        Assert.Null(TypeToResource.FetchResourceFromType(typeof(HostDerived), false, pluginService));
        Assert.Null(TypeToResource.FetchResourceFromType(typeof(HostBase), true, null));
    }

    private class Base;

    private sealed class Derived : Base;

    private class HostBase;

    private sealed class HostDerived : HostBase;

    /// <summary>
    /// A plugin service with type images only.
    /// </summary>
    private sealed class ImagesPluginService(Dictionary<Type, object> images) : IAssetsPluginService
    {
        public IReadOnlyList<AssetsPlugin> Plugins => [];

        public object? GetImageForType(Type type) => images.GetValueOrDefault(type);

        public bool HasImagesForEnum(ISessionViewModel? session, Type enumType) => false;

        public object? GetImageForEnum(ISessionViewModel? session, object value) => null;

        public bool HasEditorView(ISessionViewModel session, Type viewModelType) => false;

        public Type? GetAssetViewModelType(Type assetType) => null;

        public Type? GetEditorViewModelType(Type viewModelType) => null;

        public Type? GetEditorViewType(Type editorViewModelType) => null;

        public Type? GetPreviewViewModelType(Type previewType) => null;

        public Type? GetPreviewViewType(Type previewType) => null;

        public void RegisterSession(ISessionViewModel session, ILogger logger) => throw new NotSupportedException();

        public void InitializeSession(ISessionViewModel session) => throw new NotSupportedException();

        public IEnumerable<Type> GetPrimitiveTypes(ISessionViewModel session) => [];

        public IEnumerable<PackageSettingsEntry> GetProfileSettings() => [];
    }

    /// <summary>
    /// A plugin exposing the type images to the tests.
    /// </summary>
    private sealed class TestImagesPlugin : AssetsEditorPlugin
    {
        public static void Register(Type type, object image) => TypeImages[type] = image;
    }
}

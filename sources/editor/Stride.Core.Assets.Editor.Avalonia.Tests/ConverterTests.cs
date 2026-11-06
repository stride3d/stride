// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Avalonia;
using Stride.Core.Assets.Editor.Avalonia.Converters;
using Stride.Core.Assets.Editor.Avalonia.Tests.Helpers;
using Stride.Core.Assets.Editor.Services;
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

    private class Base;

    private sealed class Derived : Base;

    /// <summary>
    /// A plugin exposing the type images to the tests.
    /// </summary>
    private sealed class TestImagesPlugin : AssetsEditorPlugin
    {
        public static void Register(Type type, object image) => TypeImages[type] = image;
    }
}

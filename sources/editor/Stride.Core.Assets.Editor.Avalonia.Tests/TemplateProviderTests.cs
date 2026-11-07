// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Headless.XUnit;
using Stride.Core.Assets.Editor.Avalonia.Tests.Helpers;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Presentation.Avalonia.Views;
using Stride.Core.Presentation.Views;

namespace Stride.Core.Assets.Editor.Avalonia.Tests;

public sealed class TemplateProviderTests
{
    [AvaloniaFact]
    public void TheDefaultProvidersLoadWithTheirTemplates()
    {
        var providers = new DefaultPropertyTemplateProviders().Values.OfType<TemplateProviderBase>().ToList();

        Assert.True(providers.Count > 50, $"{providers.Count} providers");
        Assert.All(providers, provider => Assert.NotNull(provider.Template));
        Assert.All(providers, provider => Assert.False(string.IsNullOrEmpty(provider.Name)));
        // Every category of the property grid has providers
        Assert.Contains(providers, x => PropertyViewHelper.GetTemplateCategory(x) == PropertyViewHelper.Category.PropertyHeader);
        Assert.Contains(providers, x => PropertyViewHelper.GetTemplateCategory(x) == PropertyViewHelper.Category.PropertyEditor);
        Assert.Contains(providers, x => PropertyViewHelper.GetTemplateCategory(x) == PropertyViewHelper.Category.PropertyFooter);
        // The names the property grid relies on
        foreach (var name in new[] { "Boolean", "String", "Enum", "FlagEnum", "List", "[]", "Set", "Dictionary", "Nullable", "SimpleReference", "ThumbnailReference", "UnloadableObject" })
        {
            Assert.Contains(providers, x => x.Name == name);
        }
    }

    [AvaloniaFact]
    public void TheSelectorBuildsTheMatchingTemplateOnly()
    {
        var providers = new DefaultPropertyTemplateProviders().Values.OfType<TemplateProviderBase>().ToList();
        var selector = new PropertyTemplateProviderSelector();
        selector.RegisterTemplateProvider(providers.Single(x => x.Name == "Boolean"));
        var context = new NodeViewModelContext();
        var sample = new SampleObject();

        Assert.NotNull(selector.Build(context.MemberOf(sample, nameof(SampleObject.Flag))));
        Assert.Null(selector.Build(context.MemberOf(sample, nameof(SampleObject.Text))));
        Assert.Null(selector.Build("not a node"));
    }
}

// The template providers are AvaloniaObjects: Avalonia facts keep them on the headless session's thread. A plain fact
// runs on an xunit worker in parallel and can claim the UI dispatcher before the session starts, which fails its setup.
public sealed class TemplateProviderMatchingTests
{
    private readonly NodeViewModelContext context = new();
    private readonly SampleObject sample = new();

    [AvaloniaTheory]
    [InlineData(nameof(SampleObject.Integer), true)]
    [InlineData(nameof(SampleObject.Text), false)]
    public void TypeMatchProviderMatchesTheNodeType(string member, bool expected)
    {
        var provider = new TypeMatchTemplateProvider { Type = typeof(int), Template = null! };

        Assert.Equal(expected, provider.Match(context.MemberOf(sample, member)));
        Assert.Equal("Int32", provider.Name);
    }

    [AvaloniaFact]
    public void TypeMatchProviderWithoutTypeMatchesEveryNode()
    {
        var provider = new TypeMatchTemplateProvider { Template = null! };

        Assert.True(provider.Match(context.MemberOf(sample, nameof(SampleObject.Integer))));
        Assert.True(provider.Match(context.MemberOf(sample, nameof(SampleObject.Text))));
        Assert.Equal("Undefined", provider.Name);
    }

    [AvaloniaFact]
    public void CollectionProvidersMatchTheirKindOfCollection()
    {
        var list = context.MemberOf(sample, nameof(SampleObject.Numbers));
        var array = context.MemberOf(sample, nameof(SampleObject.Array));
        var set = context.MemberOf(sample, nameof(SampleObject.Set));
        var map = context.MemberOf(sample, nameof(SampleObject.Map));

        var listProvider = new ListTemplateProvider { Template = null! };
        var arrayProvider = new ArrayTemplateProvider { Template = null! };
        var setProvider = new SetTemplateProvider { Template = null! };
        var dictionaryProvider = new DictionaryTemplateProvider { Template = null! };

        Assert.True(listProvider.Match(list));
        Assert.False(listProvider.Match(set));
        Assert.False(listProvider.Match(map));
        Assert.True(arrayProvider.Match(array));
        Assert.False(arrayProvider.Match(list));
        Assert.True(setProvider.Match(set));
        Assert.False(setProvider.Match(list));
        Assert.True(dictionaryProvider.Match(map));
        Assert.False(dictionaryProvider.Match(list));
    }

    [AvaloniaFact]
    public void ListProviderNamesItsElementType()
    {
        var list = context.MemberOf(sample, nameof(SampleObject.Numbers));
        var provider = new ListTemplateProvider { ElementType = typeof(int), Template = null! };

        Assert.Equal("ListInt32", provider.Name);
        // An IList node matches whatever the element type; the element type only narrows other collections
        Assert.True(provider.Match(list));
        Assert.True(new ListTemplateProvider { ElementType = typeof(string), Template = null! }.Match(list));
    }

    [AvaloniaFact]
    public void EnumProvidersDistinguishFlags()
    {
        var plain = context.MemberOf(sample, nameof(SampleObject.Plain));
        var flags = context.MemberOf(sample, nameof(SampleObject.Flags));
        var integer = context.MemberOf(sample, nameof(SampleObject.Integer));

        var enumProvider = new EnumTemplateProvider { Template = null! };
        var flagProvider = new EnumTemplateProvider { FlagEnum = true, Template = null! };
        var imageProvider = new EnumTemplateProvider { ImageEnum = true, Template = null! };

        Assert.Equal("Enum", enumProvider.Name);
        Assert.Equal("FlagEnum", flagProvider.Name);
        Assert.Equal("ImageEnum", imageProvider.Name);
        Assert.True(enumProvider.Match(plain));
        Assert.True(enumProvider.Match(flags));
        Assert.False(enumProvider.Match(integer));
        Assert.False(flagProvider.Match(plain));
        Assert.True(flagProvider.Match(flags));
        // No plugin service provides enum images here
        Assert.False(imageProvider.Match(plain));
    }

    [AvaloniaFact]
    public void NullableProviderMatchesNullableStructsOnly()
    {
        var provider = new NullableTemplateProvider { Template = null! };

        Assert.True(provider.Match(context.MemberOf(sample, nameof(SampleObject.NullableStruct))));
        // Nullable primitives are handled by the typed providers (AcceptNullable)
        Assert.False(provider.Match(context.MemberOf(sample, nameof(SampleObject.NullableInteger))));
        Assert.False(provider.Match(context.MemberOf(sample, nameof(SampleObject.Integer))));
        Assert.False(provider.Match(context.MemberOf(sample, nameof(SampleObject.Text))));
    }

    [AvaloniaFact]
    public void ProvidersOnlyMatchNodes()
    {
        Assert.False(new TypeMatchTemplateProvider { Type = typeof(string), Template = null! }.Match("a string"));
        Assert.False(new ListTemplateProvider { Template = null! }.Match(null));
    }
}

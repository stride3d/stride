// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Headless.XUnit;
using Stride.Assets.Editor.Avalonia.Views;
using Stride.Core.Assets.Editor.Avalonia.Tests.Helpers;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Avalonia.Views;
using Stride.Core.Presentation.Views;
using Stride.Engine;
using Stride.Rendering;

namespace Stride.Assets.Editor.Avalonia.Tests;

public sealed class TemplateProviderTests
{
    [AvaloniaFact]
    public void TheEntityProvidersLoadWithTheirTemplates()
    {
        var providers = new EntityPropertyTemplateProviders().Values.OfType<TemplateProviderBase>().ToList();

        // The entity, component and game settings providers (the merged default providers belong to the core plugin)
        Assert.Contains(providers, x => x is EntityReferenceTemplateProvider);
        Assert.Contains(providers, x => x is EntityComponentReferenceTemplateProvider);
        Assert.Contains(providers, x => x is EntityComponentCollectionTemplateProvider);
        Assert.Contains(providers, x => x is GameSettingAddConfigurationTemplateProvider);
        Assert.All(providers, provider => Assert.NotNull(provider.Template));
        Assert.All(providers, provider => Assert.False(string.IsNullOrEmpty(provider.Name)));
    }

    [AvaloniaFact]
    public void TheViewPluginRegistersTheEntityProviders()
    {
        var expected = new EntityPropertyTemplateProviders().Values.OfType<TemplateProviderBase>().Count();
        var registered = new List<ITemplateProvider>();
        var plugin = new StrideEditorViewPlugin();

        plugin.InitializePlugin(new LoggerResult());
        plugin.RegisterTemplateProviders(registered);

        Assert.Equal(expected, registered.Count);
        Assert.Contains(registered, x => x is EntityReferenceTemplateProvider);
    }

    [AvaloniaFact]
    public void TheHeaderAndEditorCategoriesAreCovered()
    {
        var providers = new EntityPropertyTemplateProviders().Values.OfType<TemplateProviderBase>().Where(x => x.GetType().Namespace == typeof(EntityReferenceTemplateProvider).Namespace).ToList();

        Assert.Contains(providers, x => PropertyViewHelper.GetTemplateCategory(x) == PropertyViewHelper.Category.PropertyHeader);
        Assert.Contains(providers, x => PropertyViewHelper.GetTemplateCategory(x) == PropertyViewHelper.Category.PropertyEditor);
        Assert.Contains(providers, x => PropertyViewHelper.GetTemplateCategory(x) == PropertyViewHelper.Category.PropertyFooter);
    }
}

public sealed class TemplateProviderMatchingTests
{
    private readonly NodeViewModelContext context = new();

    public sealed class Holder
    {
        public Entity? Entity { get; set; }

        public TransformComponent? Component { get; set; }

        // An interface the engine implements on a component (ModelComponent)
        public IModelInstance? Interface { get; set; }

        public string? Text { get; set; }

        public Entity Owner { get; set; } = new("Owner");

        public GameSettingsAsset Settings { get; set; } = new();
    }


    [Fact]
    public void TheEntityReferenceProviderMatchesEntityNodes()
    {
        var provider = new EntityReferenceTemplateProvider { Template = null! };
        var holder = new Holder();

        Assert.True(provider.Match(context.MemberOf(holder, nameof(Holder.Entity))));
        Assert.False(provider.Match(context.MemberOf(holder, nameof(Holder.Component))));
        Assert.False(provider.Match(context.MemberOf(holder, nameof(Holder.Text))));
        Assert.False(provider.Match("not a node"));
        Assert.Equal("EntityReference", provider.Name);
    }

    [Fact]
    public void TheComponentReferenceProviderMatchesComponentsAndTheirInterfaces()
    {
        var provider = new EntityComponentReferenceTemplateProvider { Template = null! };
        var holder = new Holder();

        Assert.True(provider.Match(context.MemberOf(holder, nameof(Holder.Component))));
        Assert.True(provider.Match(context.MemberOf(holder, nameof(Holder.Interface))));
        Assert.False(provider.Match(context.MemberOf(holder, nameof(Holder.Entity))));
        Assert.False(provider.Match(context.MemberOf(holder, nameof(Holder.Text))));
        // The components of an entity are not references
        var components = context.MemberOf(holder, nameof(Holder.Owner)).GetChild(nameof(Entity.Components));
        Assert.NotNull(components);
        Assert.All(components.Children, x => Assert.False(provider.Match(x)));
    }

    [Fact]
    public void TheComponentCollectionProviderMatchesTheComponentsOfAnEntity()
    {
        var provider = new EntityComponentCollectionTemplateProvider { Template = null! };
        var holder = new Holder();

        var components = context.MemberOf(holder, nameof(Holder.Owner)).GetChild(nameof(Entity.Components));
        Assert.NotNull(components);
        Assert.True(provider.Match(components));
        Assert.False(provider.Match(context.MemberOf(holder, nameof(Holder.Owner))));
    }

    [Fact]
    public void TheAddConfigurationProviderMatchesTheDefaultsOfTheGameSettings()
    {
        var provider = new GameSettingAddConfigurationTemplateProvider { Template = null! };
        var holder = new Holder();

        var settings = context.MemberOf(holder, nameof(Holder.Settings));
        Assert.True(provider.Match(settings.GetChild(nameof(GameSettingsAsset.Defaults))!));
        Assert.False(provider.Match(settings));
    }
}

// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Entities;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.EntityFactories;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Engine;

namespace Stride.Assets.Presentation.Common.Tests;

/// <summary>
/// The entity factories and add-asset policies of the plugins derive from toolkit-neutral base classes, which see the
/// editor item they work on through <see cref="IEntityHierarchyItemViewModel"/>.
/// </summary>
public class TestEntityFactories
{
    private sealed class FakeHierarchyItem : IEntityHierarchyItemViewModel
    {
        public AssetViewModel Asset => null!;
    }

    private sealed class LightFactory : EntityFactory
    {
        public override Task<Entity> CreateEntity(IEntityHierarchyItemViewModel parent)
        {
            var name = ComputeNewName(parent, "Light");
            return CreateEntityWithComponent(name, new LightComponent(), new ModelComponent());
        }
    }

    private sealed class AddPrefabPolicy : CreateComponentPolicyBase<PrefabAsset, AssetViewModel<PrefabAsset>>
    {
        public IEntityHierarchyItemViewModel? Parent { get; private set; }

        protected override EntityComponent CreateComponentFromAsset(IEntityHierarchyItemViewModel parent, AssetViewModel<PrefabAsset> asset)
        {
            Parent = parent;
            return new ModelComponent();
        }
    }

    [Fact]
    public async Task FactoriesCreateEntitiesWithTheirComponents()
    {
        var entity = await new LightFactory().CreateEntity(new FakeHierarchyItem());

        Assert.Equal("Light", entity.Name);
        Assert.Contains(entity.Components, x => x is LightComponent);
        Assert.Contains(entity.Components, x => x is ModelComponent);
        Assert.Single(entity.Components, x => x is TransformComponent);
    }

    [Fact]
    public void PoliciesAcceptTheirAssetTypeOnly()
    {
        var policy = new AddPrefabPolicy();

        Assert.True(policy.Accept(typeof(PrefabAsset)));
        Assert.False(policy.Accept(typeof(SceneAsset)));
        Assert.Throws<ArgumentNullException>(() => policy.Accept(null!));
    }

    [Fact]
    public void ComponentPoliciesCreateTheComponentForTheItem()
    {
        var policy = new AddPrefabPolicy();
        var parent = new FakeHierarchyItem();

        var accepted = ((IAddAssetPolicy)policy).CanAddOrInsert(parent, null!, AddChildModifiers.None, 0, out var message, "the scene");
        var component = ((ICreateComponentPolicy)policy).CreateComponentFromAsset(parent, null!);

        Assert.True(accepted);
        Assert.Equal("Add the selection into the scene", message);
        Assert.IsType<ModelComponent>(component);
        Assert.Same(parent, policy.Parent);
    }
}

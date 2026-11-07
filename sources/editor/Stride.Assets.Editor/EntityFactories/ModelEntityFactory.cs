// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Models;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.EntityFactories;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Core.Assets.Editor.Services;
using Stride.Core;
using Stride.Engine;
using Stride.Rendering;

namespace Stride.Assets.Editor.EntityFactories;

[Display(10, "Model", "Model")]
public sealed class ModelEntityFactory : EntityFactory
{
    [ModuleInitializer]
    internal static void RegisterCategory()
    {
        EntityFactoryCategory.RegisterCategory(10, "Model");
    }

    public override async Task<Entity?> CreateEntity(IEntityHierarchyItemViewModel parent)
    {
        var model = await EntityFactoryHelper.PickupAsset(parent.Asset.Session, new[] { typeof(IModelAsset) });
        if (model == null)
            return null;

        var name = ComputeNewName(parent, model.Name);
        var component = new ModelComponent { Model = ContentReferenceHelper.CreateReference<Model>(model) };
        return await CreateEntityWithComponent(name, component);
    }
}

[Display(30, "Instanced Model", "Model")]
public sealed class InstancedModelEntityFactory : EntityFactory
{
    public override async Task<Entity?> CreateEntity(IEntityHierarchyItemViewModel parent)
    {
        var model = await EntityFactoryHelper.PickupAsset(parent.Asset.Session, new[] { typeof(IModelAsset) });
        if (model == null)
            return null;

        var name = ComputeNewName(parent, model.Name);
        var component = new ModelComponent { Model = ContentReferenceHelper.CreateReference<Model>(model) };
        var instancingComponent = new InstancingComponent();
        return await CreateEntityWithComponent(name, component, instancingComponent);
    }
}

[Display(40, "Model Instance", "Model")]
public sealed class InstanceEntityFactory : EntityFactory
{
    public override Task<Entity> CreateEntity(IEntityHierarchyItemViewModel parent)
    {
        var name = ComputeNewName(parent, "Instance");
        var component = new InstanceComponent();
        return CreateEntityWithComponent(name, component);
    }
}

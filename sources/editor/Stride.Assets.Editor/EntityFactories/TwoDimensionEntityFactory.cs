// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.EntityFactories;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Core;
using Stride.Engine;

namespace Stride.Assets.Editor.EntityFactories;

[Display(10, "Sprite", "2D")]
public sealed class SpriteEntityFactory : EntityFactory
{
    [ModuleInitializer]
    internal static void RegisterCategory()
    {
        EntityFactoryCategory.RegisterCategory(40, "2D");
    }

    public override Task<Entity> CreateEntity(IEntityHierarchyItemViewModel parent)
    {
        var name = ComputeNewName(parent, "Sprite");
        var component = new SpriteComponent();
        return CreateEntityWithComponent(name, component);
    }
}

[Display(30, "Background", "2D")]
public sealed class BackgroundEntityFactory : EntityFactory
{
    public override Task<Entity> CreateEntity(IEntityHierarchyItemViewModel parent)
    {
        var name = ComputeNewName(parent, "Background");
        var component = new BackgroundComponent();
        return CreateEntityWithComponent(name, component);
    }
}

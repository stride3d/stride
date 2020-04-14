// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;
using Stride.Core;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Engine;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.EntityFactories
{
    [Display(10, "Sprite", "2D")]
    public class SpriteEntityFactory : EntityFactory
    {
        [ModuleInitializer]
        internal static void RegisterCategory()
        {
            EntityFactoryCategory.RegisterCategory(40, "2D");
        }

        public override Task<Entity> CreateEntity(EntityHierarchyItemViewModel parent)
        {
            var name = ComputeNewName(parent, "Sprite");
            var component = new SpriteComponent();
            return CreateEntityWithComponent(name, component);
        }
    }

    [Display(20, "UI", "2D")]
    public class UIEntityFactory : EntityFactory
    {
        public override Task<Entity> CreateEntity(EntityHierarchyItemViewModel parent)
        {
            var name = ComputeNewName(parent, "UI");
            var component = new UIComponent();
            return CreateEntityWithComponent(name, component);
        }
    }

    [Display(30, "Background", "2D")]
    public class BackgroundEntityFactory : EntityFactory
    {
        public override Task<Entity> CreateEntity(EntityHierarchyItemViewModel parent)
        {
            var name = ComputeNewName(parent, "Background");
            var component = new BackgroundComponent();
            return CreateEntityWithComponent(name, component);
        }
    }

    [Display(40, "SpriteStudio", "2D")]
    // FIXME: this view model should be in the SpriteStudio offline assembly! Can't be done now, because of a circular reference in CompilerApp referencing SpriteStudio, and Editor referencing CompilerApp
    public class SpriteStudioFactory : EntityFactory
    {
        public override Task<Entity> CreateEntity(EntityHierarchyItemViewModel parent)
        {
            var name = ComputeNewName(parent, "SpriteStudio");
            var component = new SpriteStudioComponent();
            return CreateEntityWithComponent(name, component);
        }
    }
}

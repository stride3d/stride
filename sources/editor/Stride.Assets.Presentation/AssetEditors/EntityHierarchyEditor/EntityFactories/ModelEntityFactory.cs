// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;
using Xenko.Core.Assets.Editor.Services;
using Xenko.Core;
using Xenko.Assets.Models;
using Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Xenko.Engine;
using Xenko.Rendering;

namespace Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.EntityFactories
{
    [Display(10, "Model", "Model")]
    public class ModelEntityFactory : EntityFactory
    {
        [ModuleInitializer]
        internal static void RegisterCategory()
        {
            EntityFactoryCategory.RegisterCategory(10, "Models");
        }

        public override async Task<Entity> CreateEntity(EntityHierarchyItemViewModel parent)
        {
            var model = await PickupAsset(parent.Editor.Session, new [] { typeof(IModelAsset) });
            if (model == null)
                return null;

            var name = ComputeNewName(parent, model.Name);
            var component = new ModelComponent { Model = ContentReferenceHelper.CreateReference<Model>(model) };
            return await CreateEntityWithComponent(name, component);
        }
    }
}

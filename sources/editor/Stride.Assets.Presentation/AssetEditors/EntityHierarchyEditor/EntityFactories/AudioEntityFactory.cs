// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;
using Stride.Core;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Engine;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.EntityFactories
{
    [Display(10, "Audio emitter", "Audio")]
    public class AudioEmitterFactory : EntityFactory
    {
        [ModuleInitializer]
        internal static void RegisterCategory()
        {
                EntityFactoryCategory.RegisterCategory(50, "Audio");
        }

        public override Task<Entity> CreateEntity(EntityHierarchyItemViewModel parent)
        {
            var name = ComputeNewName(parent, "AudioEmitter");
            var component = new AudioEmitterComponent();
            return CreateEntityWithComponent(name, component);
        }
    }

    [Display(20, "Audio listener", "Audio")]
    public class AudioListenerFactory : EntityFactory
    {
        public override Task<Entity> CreateEntity(EntityHierarchyItemViewModel parent)
        {
            var name = ComputeNewName(parent, "AudioListener");
            var component = new AudioListenerComponent();
            return CreateEntityWithComponent(name, component);
        }
    }
}

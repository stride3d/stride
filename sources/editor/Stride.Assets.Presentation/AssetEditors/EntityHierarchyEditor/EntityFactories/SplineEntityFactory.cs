using System.Threading.Tasks;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Splines.Components;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.EntityFactories
{
    [Display(10, "Spline", "Spline")]
    public class SplineFactory : EntityFactory
    {
        [ModuleInitializer]
        internal static void RegisterCategory()
        {
                EntityFactoryCategory.RegisterCategory(50, "Spline");
        }

        public override Task<Entity> CreateEntity(EntityHierarchyItemViewModel parent)
        {
            return CreateEntityWithComponent("Spline", new SplineComponent());
        }
    }

    [Display(20, "Spline node", "Spline")]
    public class SplineNodeFactory : EntityFactory
    {
        public override Task<Entity> CreateEntity(EntityHierarchyItemViewModel parent)
        {
            var name = ComputeNewName(parent, "SplineNode");
            var component = new SplineNodeComponent(50);
            return CreateEntityWithComponent(name, component);
        }
    }
}

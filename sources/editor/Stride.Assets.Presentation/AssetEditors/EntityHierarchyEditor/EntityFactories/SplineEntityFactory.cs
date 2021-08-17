using System.Threading.Tasks;
using Stride.Core;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
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
            //var splineNode1 = CreateEntityWithComponent("SplineNode1", new SplineComponent());
            //var splineNode2 = CreateEntityWithComponent("SplineNode2", new SplineComponent());
            //splineNode2.Result.Transform.Position = new Core.Mathematics.Vector3(0, 0, 1);

            var spline = CreateEntityWithComponent("Spline", new SplineComponent());
            //spline.Result.AddChild(splineNode1.Result);
            //spline.Result.AddChild(splineNode2.Result);

            return spline;
        }
    }

    [Display(20, "Spline node", "Spline")]
    public class SplineNodeFactory : EntityFactory
    {
        public override Task<Entity> CreateEntity(EntityHierarchyItemViewModel parent)
        {
            var name = ComputeNewName(parent, "SplineNode");
            var component = new SplineNodeComponent();
            return CreateEntityWithComponent(name, component);
        }
    }
}

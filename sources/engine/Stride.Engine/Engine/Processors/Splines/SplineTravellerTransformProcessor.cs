using Stride.Engine.Splines;
using Stride.Engine.Splines.Components;

namespace Stride.Engine.Processors
{
    /// <summary>
    /// The processor for <see cref="SplineFollowerComponent"/>.
    /// </summary>
    public class SplineTravellerTransformProcessor : EntityProcessor<SplineTravellerComponent, SplineTravellerTransformProcessor.SplineTravellerTransformationInfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SplineTransformProcessor"/> class.
        /// </summary>
        public SplineTravellerTransformProcessor()
            : base(typeof(TransformComponent))
        {
        }

        protected override SplineTravellerTransformationInfo GenerateComponentData(Entity entity, SplineTravellerComponent component)
        {
            return new SplineTravellerTransformationInfo
            {
                TransformOperation = new SplineTravellerViewHierarchyTransformOperation(component),
            };
        }

        protected override bool IsAssociatedDataValid(Entity entity, SplineTravellerComponent component, SplineTravellerTransformationInfo associatedData)
        {
            return component == associatedData.TransformOperation.SplineTravellerComponent;
        }

        protected override void OnEntityComponentAdding(Entity entity, SplineTravellerComponent component, SplineTravellerTransformationInfo data)
        {
            // Register model view hierarchy update
            entity.Transform.PostOperations.Add(data.TransformOperation);
        }

        protected override void OnEntityComponentRemoved(Entity entity, SplineTravellerComponent component, SplineTravellerTransformationInfo data)
        {
            // Unregister model view hierarchy update
            entity.Transform.PostOperations.Remove(data.TransformOperation);
        }

        public class SplineTravellerTransformationInfo
        {
            public SplineTravellerViewHierarchyTransformOperation TransformOperation;
        }
    }
}

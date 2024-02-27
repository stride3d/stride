//// Copyright (c) Stride contributors (https://Stride.com)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Engine.Splines.Components;
using Stride.Engine.Splines.HierarchyTransformOperations;
using Stride.Engine.Splines.Models.Decorators;
using Stride.Engine.Splines.Processors.DecoratorProcessors;
using Stride.Games;

namespace Stride.Engine.Splines.Processors
{
    /// <summary>
    /// The processor for <see cref="SplineDecoratorComponent"/>.
    /// </summary>
    public class SplineDecoratorProcessor : EntityProcessor<SplineDecoratorComponent, SplineDecoratorProcessor.SplineDecoratorTransformationInfo>
    {
        private HashSet<SplineDecoratorComponent> splineDecoratorComponentsToUpdate = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="SplineDecoratorProcessor"/> class.
        /// </summary>
        public SplineDecoratorProcessor()
            : base(typeof(TransformComponent))
        {
        }

        protected override SplineDecoratorTransformationInfo GenerateComponentData(Entity entity, SplineDecoratorComponent component)
        {
            return new SplineDecoratorTransformationInfo
            {
                TransformOperation = new SplineDecoratorViewHierarchyTransformOperation(component),
            };
        }

        protected override bool IsAssociatedDataValid(Entity entity, SplineDecoratorComponent component, SplineDecoratorTransformationInfo associatedData)
        {
            return component == associatedData.TransformOperation.SplineDecoratorComponent;
        }

        protected override void OnEntityComponentAdding(Entity entity, SplineDecoratorComponent component, SplineDecoratorTransformationInfo data)
        {
            //Every time the spline decorator is marked as dirty, we want to re-decorate the spline
            component.OnSplineDecoratorDirty += () => data.Update(splineDecoratorComponentsToUpdate, component);

            splineDecoratorComponentsToUpdate.Add(component);
            
            entity.Transform.PostOperations.Add(data.TransformOperation);
        }
        
        protected override void OnEntityComponentRemoved(Entity entity, SplineDecoratorComponent component, SplineDecoratorTransformationInfo data)
        {
            splineDecoratorComponentsToUpdate.Add(component);

            component.OnSplineDecoratorDirty -= () => data.Update(splineDecoratorComponentsToUpdate, component);
            
            entity.Transform.PostOperations.Remove(data.TransformOperation);
            // splineDecoratorComponentsToUpdate.Remove(component);
        }

        public class SplineDecoratorTransformationInfo
        {
            public SplineDecoratorViewHierarchyTransformOperation TransformOperation;
            
            public void Update(HashSet<SplineDecoratorComponent>splineDecoratorComponentsToUpdate, SplineDecoratorComponent component)
            {
                splineDecoratorComponentsToUpdate.Add(component);
            }
        }

        public override void Update(GameTime time)
        {
            foreach (var splineDecoratorComponent in splineDecoratorComponentsToUpdate)
            {
                if (splineDecoratorComponent?.DecoratorSettings == null)
                    continue;

                splineDecoratorComponent.ClearDecorationInstances();
               
                BaseSplineDecoratorProcessor baseSplineDecoratorProcessor = splineDecoratorComponent.DecoratorSettings switch
                    {
                        SplineAmountDecoratorSettings => new AmountDecoratorProcessor(),
                        SplineIntervalDecoratorSettings => new IntervalDecoratorProcessor(),
                        _ => throw new InvalidOperationException($"Unsupported SplineDecoratorSettings type {splineDecoratorComponent.DecoratorSettings}")
                    };
                
                baseSplineDecoratorProcessor.Decorate(splineDecoratorComponent);
                
            }
            
            //Now that dirty spline decorators have been updated, clear the collection
            splineDecoratorComponentsToUpdate.Clear();
        }
    }
}

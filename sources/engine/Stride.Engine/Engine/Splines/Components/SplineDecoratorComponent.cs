//// Copyright (c) Stride contributors (https://Stride.com)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core;
using Stride.Engine.Design;
using Stride.Engine.Splines.Models.Decorators;
using Stride.Engine.Splines.Processors;

namespace Stride.Engine.Splines.Components
{
    /// <summary>
    /// Component representing a Spline decorator.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This component allows distribution of prefabs along the spline
    /// </para>
    /// </remarks>
    [DataContract("SplineDecoratorComponent")]
    [Display("Spline decorator", Expand = ExpandRule.Once)]
    [DefaultEntityComponentProcessor(typeof(SplineDecoratorProcessor))]
    [ComponentCategory("Splines")]
    public sealed class SplineDecoratorComponent : EntityComponent
    {
        private SplineComponent splineComponent;
        private List<Entity> decorationInstances = new List<Entity>();
        private SplineDecoratorSettings decoratorSettings;

        [Display(10, "SplineComponent")]
        public SplineComponent SplineComponent
        {
            get { return splineComponent; }
            set
            {
                if (value == null && splineComponent?.Spline != null )
                {
                    splineComponent.Spline.OnSplineUpdated -= EnqueueSplineDecoratorUpdate;
                }
                
                splineComponent = value;
                if (splineComponent != null)
                {
                    splineComponent.Spline.OnSplineUpdated += EnqueueSplineDecoratorUpdate;
                }
                
                EnqueueSplineDecoratorUpdate();
            }
        }
        
        /// <summary>
        /// Event triggered when the splineDecorator has become dirty
        /// </summary>
        public delegate void DirtySplineDecoratorHandler();

        public event DirtySplineDecoratorHandler OnSplineDecoratorDirty;

        /// <summary>
        /// Invokes the Spline Traverser Update event
        /// </summary>
        private void EnqueueSplineDecoratorUpdate()
        {
            OnSplineDecoratorDirty?.Invoke();
        }

        public SplineDecoratorComponent()
        {
        }

        public SplineDecoratorComponent(SplineDecoratorSettings decoratorSettings)
        {
            this.decoratorSettings = decoratorSettings;
        }

        /// <summary>
        /// Decorator settings of the decorator components
        /// </summary>
        [DataMember(40)]
        [Display("Decorator settings")]
        public SplineDecoratorSettings DecoratorSettings
        {
            get { return decoratorSettings; }
            set
            {
                decoratorSettings = value;
                EnqueueSplineDecoratorUpdate();
            }
        }

        /// <summary>
        /// All entity instances created and decorated along the ppline
        /// </summary>
        [DataMemberIgnore]
        public List<Entity> DecorationInstances
        {
            get { return decorationInstances; }
            set
            {
                decorationInstances = value;
            }
        }

        public void ClearDecorationInstances()
        {
            if (decorationInstances == null)
            {
                return;
            }

            foreach (var decorationInstance in decorationInstances)
            {
                Entity?.RemoveChild(decorationInstance);
                decorationInstance.Dispose();
            }

            decorationInstances.Clear();
        }

        internal void Update(TransformComponent transformComponent)
        {
        }
    }
}

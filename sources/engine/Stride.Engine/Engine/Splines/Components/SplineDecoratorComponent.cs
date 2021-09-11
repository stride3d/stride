using Stride.Core;
using Stride.Engine.Design;
using Stride.Engine.Processors;
using System;
using System.Collections.Generic;
using Stride.Engine.Splines.Models;

namespace Stride.Engine.Splines.Components
{
    /// <summary>
    /// Component representing a Spline decorator.
    /// </summary>
    [DataContract("SplineDecoratorComponent")]
    [Display("SplineDecorator", Expand = ExpandRule.Once)]
    [DefaultEntityComponentProcessor(typeof(SplineDecoratorTransformProcessor))]
    [ComponentCategory("Splines")]
    public sealed class SplineDecoratorComponent : EntityComponent
    {
        private SplineComponent splineComponent;
        private List<Entity> decorationInstances = new List<Entity>();
        private DecoratorDistributionSetting distribution;

        [Display(10, "SplineComponent")]
        public SplineComponent SplineComponent
        {
            get { return splineComponent; }
            set
            {
                splineComponent = value;
                if (splineComponent != null)
                {
                    splineComponent.OnSplineUpdated += UpdateDecorator;
                    UpdateDecorator();
                }
                else
                {
                    ClearDecorationInstance();
                }
            }
        }

        /// <summary>
        /// A list of prefabs that the decorators uses to instantiate
        /// </summary>
        [Display(60, "Decorations")]
        public List<Prefab> decorations = new List<Prefab>();

        /// <summary>
        /// Decorator settings of the decorator components
        /// </summary>
        [DataMember(70)]
        [Display("Distribution")]
        public DecoratorDistributionSetting Distribution
        {
            get { return distribution; }
            set
            {
                distribution = value;
                UpdateDecorator();
            }
        }

        private void DecorateWithAmount()
        {
            ClearDecorationInstance();

            if (SplineComponent != null && SplineComponent.GetTotalSplineDistance() > 0
                && decorations.Count > 0 && Distribution is AmountDecorator AmountDecorator && AmountDecorator.Amount > 0)
            {
                var totalSplineDistance = SplineComponent.GetTotalSplineDistance();
                var segmentLength = totalSplineDistance / AmountDecorator.Amount + 1;

                for (int iteration = 1; iteration <= AmountDecorator.Amount; iteration++)
                {
                    var percentage = ((segmentLength * iteration) / totalSplineDistance) * 100;
                    CreateInstanceAndAddToScene(iteration, percentage);
                }
            }
        }

        private void DecorateWithInterval()
        {
            ClearDecorationInstance();

            if (SplineComponent != null && SplineComponent.GetTotalSplineDistance() > 0
                && decorations.Count > 0 && Distribution is IntervalDecorator IntervalDecorator)
            {
                var totalSplineDistance = SplineComponent.GetTotalSplineDistance();
                var random = new Random();
                var totalIntervalDistance = 0.0f;
                var iteration = 0;

                while (iteration < 1000) //Hardcoded 1000?
                {
                    var nextInterval = random.NextDouble() * (IntervalDecorator.Interval.Y - IntervalDecorator.Interval.X) + IntervalDecorator.Interval.X;
                    totalIntervalDistance += (float)nextInterval;

                    if (totalIntervalDistance > totalSplineDistance)
                    {
                        break;
                    }

                    var percentage = (totalIntervalDistance / totalSplineDistance) * 100;
                    CreateInstanceAndAddToScene(iteration, percentage);

                    iteration++;
                };
            }
        }

        private void CreateInstanceAndAddToScene(int iteration, float percentage)
        {
            var splinePositionInfo = SplineComponent.GetPositionOnSpline(percentage);
            var instanceRoot = new Entity("Instance " + iteration);
            var instanceEntities = decorations[0].Instantiate();

            instanceRoot.Transform.Position = EntityTransformExtensions.WorldToLocal(Entity.Transform, splinePositionInfo.Position);
            instanceRoot.Transform.UpdateWorldMatrix();

            foreach (var instanceEntity in instanceEntities)
            {
                instanceRoot.AddChild(instanceEntity);
            }

            decorationInstances.Add(instanceRoot);
            Entity.AddChild(instanceRoot);
        }

        private void UpdateDecorator()
        {
            if (Distribution == null)
                return;

            if (Distribution is AmountDecorator)
            {
                DecorateWithAmount();
            }
            {
                DecorateWithInterval();
            }
        }

        private void ClearDecorationInstance()
        {
            decorations ??= new List<Prefab>();

            if (decorationInstances != null)
            {
                foreach (var decorationInstance in decorationInstances)
                {
                    Entity.RemoveChild(decorationInstance);
                    decorationInstance.Dispose();
                }
                decorationInstances.Clear();
            }
        }

        internal void Update(TransformComponent transformComponent)
        {
        }
    }
}

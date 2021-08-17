using Stride.Core;
using Stride.Engine.Design;
using Stride.Engine.Processors;
using Stride.Core.Mathematics;
using Stride.Core.Annotations;
using System;
using System.Collections.Generic;

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
        public SplineComponent SplineComponent { get; set; }

        public List<Prefab> decorations = new List<Prefab>();

        private List<Entity> decorationInstances = new List<Entity>();



        //[Display("Offset")]
        //private Vector3 _offset = new Vector3(0,0,0);

        //public Vector3 Offset
        //{
        //    get { return _interval; }
        //    set
        //    {
        //        _interval = value;

        //        var totalSplineDistance = SplineComponent?.GetTotalSplineDistance() > 0;
        //        if (SplineComponent?.GetTotalSplineDistance() > 0)
        //        {
        //            ClearDecorationInstance();

        //            var random = new Random();
        //            var distanceLeft = true;

        //            while (distanceLeft)
        //            {
        //                var nextInterval = random.NextDouble() * (Interval.Y - Interval.X) + Interval.X;


        //                var splinePositionInfo = SplineComponent.GetPositionOnSpline(_percentage);
        //                Entity.Transform.Position = splinePositionInfo.Position;
        //                Entity.Transform.UpdateWorldMatrix();


        //            };
        //        }
        //    }
        //}

        [Display("Interval")]
        public Vector2 Interval
        {
            get { return _interval; }
            set
            {
                _interval = value;
                DecorateWithInterval();
            }
        }

        private void DecorateWithInterval()
        {
            decorations ??= new List<Prefab>();
            decorationInstances ??= new List<Entity>();
            ClearDecorationInstance();

            if (SplineComponent != null && SplineComponent.GetTotalSplineDistance() > 0 && decorations.Count > 0)
            {
                var totalSplineDistance = SplineComponent.GetTotalSplineDistance();
                var random = new Random();
                var totalIntervalDistance = 0.0f;
                var iteration = 0;
                var worldPos = SplineComponent.Entity.Transform.WorldMatrix.TranslationVector;

                while (iteration < 1000) //Hardcoded 1000?
                {
                    var nextInterval = random.NextDouble() * (Interval.Y - Interval.X) + Interval.X;
                    totalIntervalDistance += (float)nextInterval;

                    if (totalIntervalDistance > totalSplineDistance)
                    {
                        break;
                    }

                    var percentage = (totalIntervalDistance / totalSplineDistance) * 100;
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

                    iteration++;
                };

            }
        }

        private Vector2 _interval = new Vector2(1, 1);

        private void ClearDecorationInstance()
        {
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

        public SplineDecoratorComponent()
        {
        }

        internal void Initialize()
        {
        }

        internal void Update(TransformComponent transformComponent)
        {
        }
    }
}

using Stride.Core;
using Stride.Engine.Design;
using Stride.Engine.Processors;
using Stride.Core.Mathematics;
using Stride.Core.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel;

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
        [Display(100, "SplineComponent")]
        public SplineComponent SplineComponent
        {
            get { return splineComponent; }
            set
            {
                splineComponent = value;
                if (splineComponent != null)
                {
                    splineComponent.OnSplineUpdated += UpdateDecorator;
                }
                else{
                    ClearDecorationInstance();
                }
                //if (_splineComponent == null)
                //{
                //    _splineComponent.OnSplineUpdated -= UpdateDecorator;
                //    _splineComponent = null;
                //}
                //else
                //{
                //    _splineComponent = value;
                //    _splineComponent.OnSplineUpdated += UpdateDecorator;
                //}
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="ParticleSystem"/> is enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if enabled; otherwise, <c>false</c>.
        /// </value>
        [DataMember(-10)]
        [DefaultValue(true)]
        public bool Enabled { get; set; } = true;

        [Display(90, "Decorations")]

        public List<Prefab> decorations = new List<Prefab>();

        private List<Entity> decorationInstances = new List<Entity>();

        private bool useAmountInsteadOfInterval = true;


        private int amount = 2;

        [Display(80, "Amount", "Settings")]
        public int Amount
        {
            get { return amount; }
            set
            {
                amount = Math.Max(2, value);
                DecorateWithAmount();
            }
        }

        private Vector2 _interval = new Vector2(1, 1);

        [Display(70, "Interval", "Settings")]
        public Vector2 Interval
        {
            get { return _interval; }
            set
            {
                _interval = value;
                DecorateWithInterval();
            }
        }

        private void DecorateWithAmount()
        {
            useAmountInsteadOfInterval = true;
          
            ClearDecorationInstance();

            if (SplineComponent != null && SplineComponent.GetTotalSplineDistance() > 0 && decorations.Count > 0)
            {
                var totalSplineDistance = SplineComponent.GetTotalSplineDistance();
                var segmentLength = totalSplineDistance / amount;

                for (int iteration = 1; iteration < amount; iteration++)
                {
                    var percentage = ((segmentLength * iteration) / totalSplineDistance) * 100;
                    CreateInstanceAndAddToScene(iteration, percentage);
                }
            }
        }

        private void DecorateWithInterval()
        {
            useAmountInsteadOfInterval = false;
            ClearDecorationInstance();

            if (SplineComponent != null && SplineComponent.GetTotalSplineDistance() > 0 && decorations.Count > 0)
            {
                var totalSplineDistance = SplineComponent.GetTotalSplineDistance();
                var random = new Random();
                var totalIntervalDistance = 0.0f;
                var iteration = 0;
   
                while (iteration < 1000) //Hardcoded 1000?
                {
                    var nextInterval = random.NextDouble() * (Interval.Y - Interval.X) + Interval.X;
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
            if (useAmountInsteadOfInterval)
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

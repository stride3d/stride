using Stride.Core;
using Stride.Core.Annotations;
using Stride.Engine.Design;
using Stride.Engine.Processors;

namespace Stride.Engine.Splines.Components
{
    /// <summary>
    /// Component representing a Spline Traverser.
    /// </summary>
    [DataContract("SplineTraverserComponent")]
    [Display("Spline Traverser", Expand = ExpandRule.Once)]
    [DefaultEntityComponentProcessor(typeof(SplineTraverserTransformProcessor))]
    [ComponentCategory("Splines")]
    public sealed class SplineTraverserComponent : EntityComponent
    {
        [DataMemberIgnore]
        public bool Dirty { get; set; }

        private SplineComponent splineComponent;
        private float speed = 1.0f;

        /// <summary>
        /// Event triggered when the last node of the spline has been reached
        /// </summary>
        public delegate void SplineTraverserEndReachedHandler();
        public event SplineTraverserEndReachedHandler OnSplineEndReached;

        /// <summary>
        /// Event triggered when a spline node has been reached. Does not get triggered when the last node of the spline has been reached.
        /// </summary>
        /// <param name="splineNode"></param>
        public delegate void SplineTraverserNodeReachedHandler(SplineNodeComponent splineNode);
        public event SplineTraverserNodeReachedHandler OnSplineNodeReached;


        [Display(10, "SplineComponent")]
        public SplineComponent SplineComponent
        {
            get { return splineComponent; }
            set
            {
                splineComponent = value;

                if (splineComponent == null)
                {
                    IsMoving = false;
                }

                Dirty = true;
            }
        }

        [Display(20, "Speed")]
        public float Speed
        {
            get { return speed; }
            set
            {
                speed = value;

                if (speed == 0)
                {
                    IsMoving = false;
                }

                Dirty = true;
            }
        }

        [Display(40, "Moving")]
        public bool IsMoving { get; set; }

        internal void Update(TransformComponent transformComponent)
        {

        }

        public void ActivateSplineNodeReached(SplineNodeComponent splineNode)
        {
            OnSplineNodeReached?.Invoke(splineNode);
        }

        public void ActivateOnSplineEndReached()
        {
            OnSplineEndReached?.Invoke();
        }
    }
}

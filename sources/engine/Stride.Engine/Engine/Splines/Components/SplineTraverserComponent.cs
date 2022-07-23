//// Copyright (c) Stride contributors (https://Stride.com)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Engine.Design;
using Stride.Engine.Splines.Processors;

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
        /// <summary>
        /// Keeps track of whether the Traverser needs to be Resetting its origin and target spline node
        /// </summary>
        [DataMemberIgnore]
        public bool Dirty { get; set; }

        private SplineComponent splineComponent;
        private float speed = 1.0f;

        /// <summary>
        /// Event triggered when the last node of the spline has been reached
        /// Does not get triggerd if spline loops
        /// </summary>
        public delegate void SplineTraverserEndReachedHandler();
        public event SplineTraverserEndReachedHandler OnSplineEndReached;

        /// <summary>
        /// Event triggered when a spline node has been reached. 
        /// Does not get triggered when the last node of the spline has been reached and the spline doesn't loop.
        /// </summary>
        /// <param name="splineNode"></param>
        public delegate void SplineTraverserNodeReachedHandler(SplineNodeComponent splineNode);
        public event SplineTraverserNodeReachedHandler OnSplineNodeReached;

        /// <summary>
        /// The spline to traverse
        /// No spline, no movement
        /// </summary>
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

        /// <summary>
        /// The speed at which the traverser moves over the spline
        /// Use a negative value, to go in to the opposite direction
        /// Note: Using a high value, can cause jitters. With a higher speed value, it is recommended to reduced the amount of spline points
        /// </summary>
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

        /// <summary>
        /// For a traverse to work we require a Spline reference, a non-zero and IsMoving must be True
        /// </summary>
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

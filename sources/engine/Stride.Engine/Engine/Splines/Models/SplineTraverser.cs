//// Copyright (c) Stride contributors (https://Stride.com)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Games;

namespace Stride.Engine.Splines.Models
{
    [DataContract]
    public class SplineTraverser
    {
        public Spline spline;
        public Entity entity;
        public float speed = 1.0f;
        public bool isMoving = false;
        public bool isRotating = false;
        public float thresholdDistance = 0.05f;

        public SplineNode originSplineNode { get; set; }

        public SplineNode targetSplineNode { get; set; }
        public int targetSplineNodeIndex = 0;

        public bool AttachedToSpline
        {
            get;
            set;
        }

        public BezierPoint[] bezierPointsToTraverse = null;

        public BezierPoint targetBezierPoint { get; set; }
        public BezierPoint originBezierPoint { get; set; }
        public int bezierPointIndex = 0;
        public Quaternion startRotation;

        /// <summary>
        /// The entity that is traversing the spline
        /// </summary>
        public Entity Entity
        {
            get { return entity; }
            set
            {
                entity = value;

                if (entity == null)
                {
                    isMoving = false;
                }
                else
                {
                    EnqueueSplineTraverserUpdate();
                }
            }
        }

        /// <summary>
        /// The spline to traverse
        /// No spline, no movement
        /// </summary>
        public Spline Spline
        {
            get { return spline; }
            set
            {
                spline = value;

                if (spline == null)
                {
                    isMoving = false;
                }
                else
                {
                    spline.OnSplineUpdated += EnqueueSplineTraverserUpdate;
                    EnqueueSplineTraverserUpdate();
                }
            }
        }

        /// <summary>
        /// The speed at which the traverser moves over the spline
        /// Use a negative value, to go in to the opposite direction
        /// Note: Using a high value, can cause jitters. With a higher speed value, it is recommended to reduced the amount of spline points
        /// </summary>
        public float Speed
        {
            get { return speed; }
            set
            {
                speed = value;

                EnqueueSplineTraverserUpdate();
            }
        }

        /// <summary>
        /// Determines whether the spline traver is moving
        /// For a traverser to work we require a Spline reference, a non-zero and IsMoving must be True
        /// </summary>
        public bool IsMoving
        {
            get
            {
                return isMoving;
            }
            set
            {
                isMoving = value;

                if (isMoving)
                {
                    EnqueueSplineTraverserUpdate();
                }
            }
        }

        /// <summary>
        /// Determines whether the spline traver rotates along the spline
        /// For a traverse to work we require a Spline reference, a non-zero and IsMoving must be True
        /// </summary>
        public bool IsRotating
        {
            get
            {
                return isRotating;
            }
            set
            {
                isRotating = value;
            }
        }

        /// <summary>
        /// Event triggered when the last node of the spline has been reached
        /// Does not get triggerd if spline loops
        /// </summary>
        public delegate void SplineTraverserEndReachedHandler(SplineNode splineNode);

        public event SplineTraverserEndReachedHandler OnSplineEndReached;

        public void SplineEndReached(SplineNode node)
        {
            OnSplineEndReached?.Invoke(targetSplineNode);
        }

        
        /// <summary>
        /// Event triggered when a spline node has been reached. 
        /// Does not get triggered when the last node of the spline has been reached and the spline doesn't loop.
        /// </summary>
        /// <param name="splineNode"></param>
        public delegate void SplineTraverserNodeReachedHandler(SplineNode splineNode);

        public event SplineTraverserNodeReachedHandler OnSplineNodeReached;

        public void SplineNodeReached(SplineNode node)
        {
            OnSplineNodeReached?.Invoke(targetSplineNode);
        }
        
        /// <summary>
        /// Event triggered when the splineTraverser has become dirty
        /// </summary>
        public delegate void DirtySplineHandler();

        public event DirtySplineHandler OnSplineTraverserDirty;

        public SplineTraverser()
        {
        }

        /// <summary>
        /// Invokes the Spline Traverser Update event
        /// </summary>
        public void EnqueueSplineTraverserUpdate()
        {
            OnSplineTraverserDirty?.Invoke();
        }


        public void Update(GameTime time)
        {
        }
    }
}

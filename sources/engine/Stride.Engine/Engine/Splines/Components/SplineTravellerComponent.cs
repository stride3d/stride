using Stride.Core;
using Stride.Core.Annotations;
using Stride.Engine.Design;
using Stride.Engine.Processors;
//using Stride.Editor.EditorGame.Game;

namespace Stride.Engine.Splines.Components
{
    /// <summary>
    /// Component representing a Spline traveller.
    /// </summary>
    [DataContract("SplineTravellerComponent")]
    [Display("SplineTraveller", Expand = ExpandRule.Once)]
    [DefaultEntityComponentProcessor(typeof(SplineTravellerTransformProcessor))]
    [ComponentCategory("Splines")]
    public sealed class SplineTravellerComponent : EntityComponent
    {
        /// <summary>
        /// Event triggered when the last node of the spline has been reached
        /// </summary>
        public delegate void SplineTravellerEndReachedHandler();
        public event SplineTravellerEndReachedHandler OnSplineEndReached;

        ///// <summary>
        ///// Event triggered when a spline node has been reached. Does not get triggered when the last node of the spline has been reached.
        ///// </summary>
        ///// <param name="splineNode"></param>
        public delegate void SplineTravellerNodeReachedHandler(SplineNodeComponent splineNode);
        public event SplineTravellerNodeReachedHandler OnSplineNodeReached;

        //[DataMember(1)]
        //[Display("Editor control")]
        //public SplineTravellerControl Control = new SplineTravellerControl();

        [Display(10, "SplineComponent")]
        public SplineComponent SplineComponent { get; set; }

        [Display(20, "Speed")]
        public float Speed { get; set; } = 0.1f;

        [Display(40, "Moving")]
        public bool IsMoving { get; set; }

        [Display(50, "InReverse")]
        public bool IsReverseTravelling { get; set; }

        [DataMemberRange(0.0f, 100.0f, 0.1f, 1.0f, 4)]
        [Display(70, "Percentage")]
        public float Percentage { get; set; }
       
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

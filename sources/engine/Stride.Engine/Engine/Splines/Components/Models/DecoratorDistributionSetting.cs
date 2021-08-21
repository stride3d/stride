using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Engine.Splines.Models
{
    [DataContract("Decorator settings")]
    public abstract class DecoratorDistributionSetting
    {
    }

    [DataContract("IntervalDecorator")]
    [Display("Interval")]
    public class IntervalDecorator : DecoratorDistributionSetting
    {
        /// <summary>
        /// The random distribution of instantiated prefabs along a spline <see cref="AmountDecorator"/>
        /// </summary>
        /// <userdoc>
        /// Each instance of a decorator prefab is placed alongside the spline, as long as the spline length is not covered, new instances will be made
        /// </userdoc>
        [Display("Interval")]
        public Vector2 Interval { get; set; } = new Vector2(1, 1);
    }

    [DataContract("AmountDecorator")]
    [Display("Amount")]
    public class AmountDecorator : DecoratorDistributionSetting
    {
        /// <summary>
        /// The fixed amount of decorations to be instantiated on a spline <see cref="AmountDecorator"/>
        /// </summary>
        /// <userdoc>
        /// The fixed amount of decorations to be instantiated on a spline
        /// </userdoc>
        [Display("Distance")]
        public int Amount { get; set; } = 4;
    }
}

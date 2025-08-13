using System.ComponentModel;

namespace Stride.UI.Attributes
{
    public class DefaultThicknessValueAttribute : DefaultValueAttribute
    {
        /// <summary>
        /// Initializes a new instance of the Thickness structure that has specific lengths applied to each side of the cuboid.
        /// </summary>
        /// <param name="bottom">The thickness for the lower side of the cuboid.</param>
        /// <param name="left">The thickness for the left side of the cuboid.</param>
        /// <param name="right">The thickness for the right side of the cuboid</param>
        /// <param name="top">The thickness for the upper side of the cuboid.</param>
        public DefaultThicknessValueAttribute(float left, float top, float right, float bottom)
           : base(null)
        {
            Bottom = bottom;
            Left = left;
            Right = right;
            Top = top;
        }
        

        /// <summary>
        /// The bottom side of the bounding rectangle.
        /// </summary>
        public float Bottom;

        /// <summary>
        /// The left side of the bounding rectangle.
        /// </summary>
        public float Left { get; }

        /// <summary>
        /// The right side of the bounding rectangle.
        /// </summary>
        public float Right { get; }

        /// <summary>
        /// The upper side of the bounding rectangle.
        /// </summary>
        public float Top { get; }

        public override object Value => new Thickness(Left, Top, Right, Bottom);
    }
}

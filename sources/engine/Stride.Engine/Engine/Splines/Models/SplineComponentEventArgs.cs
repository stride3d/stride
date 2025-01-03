using System;
using Stride.Engine.Splines.Components;

namespace Stride.Engine.Splines.Models
{
    public class SplineComponentEventArgs : EventArgs
    {
        public SplineComponentEventArgs(SplineComponent component)
        {
            Component = component;
        }

        public SplineComponent Component { get; set; }
    }
}

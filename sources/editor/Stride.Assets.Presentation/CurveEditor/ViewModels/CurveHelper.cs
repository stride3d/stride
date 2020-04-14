// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core.Annotations;

namespace Stride.Assets.Presentation.CurveEditor.ViewModels
{
    using WindowsPoint = System.Windows.Point;

    internal static class CurveHelper
    {
        public static ControlPointViewModelBase GetClosestPoint([ItemNotNull, NotNull]  IEnumerable<ControlPointViewModelBase> points, WindowsPoint position, double maximumDistance = double.PositiveInfinity)
        {
            ControlPointViewModelBase closest = null;
            var closestDistance = double.MaxValue;
            foreach (var p in points)
            {
                var distance = (p.ActualPoint - position).LengthSquared;
                if (distance < closestDistance && distance < maximumDistance * maximumDistance)
                {
                    closest = p;
                    closestDistance = distance;
                }
            }
            return closest;
        }
    }
}

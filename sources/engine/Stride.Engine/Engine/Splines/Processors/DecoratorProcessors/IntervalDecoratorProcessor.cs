// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Engine.Splines.Components;
using Stride.Engine.Splines.Models.Decorators;

namespace Stride.Engine.Splines.Processors.DecoratorProcessors;

public class IntervalDecoratorProcessor : BaseSplineDecoratorProcessor
{
    /// <summary>
    /// Decorates the given spline from the Spline component, using an interval
    /// The interval can be random
    /// Maximum number of prefabs to spawn is 1000
    /// </summary>
    public override void Decorate(SplineDecoratorComponent component)
    {
        if (component.Entity == null
            || component.SplineComponent == null
            || component.SplineComponent.Spline.TotalSplineDistance <= 0
            || component.DecoratorSettings.Decorations.Count <= 0 
            || component.DecoratorSettings is not SplineIntervalDecoratorSettings intervalDecoratorSettings)
        {
            return;
        }

        var totalSplineDistance = component.SplineComponent.Spline.TotalSplineDistance;
        var random = new Random();
        var totalIntervalDistance = 0.0f;
        var iteration = 0;

        while (iteration < 1000)
        {
            var nextInterval = random.NextDouble() * (intervalDecoratorSettings.Interval.Y - intervalDecoratorSettings.Interval.X) + intervalDecoratorSettings.Interval.X;
            totalIntervalDistance += (float)nextInterval;

            if (totalIntervalDistance > totalSplineDistance)
            {
                break;
            }

            var percentage = (totalIntervalDistance / totalSplineDistance) * 100;
            CreateInstance(component, iteration, percentage, random);

            iteration++;
        }
    }
}

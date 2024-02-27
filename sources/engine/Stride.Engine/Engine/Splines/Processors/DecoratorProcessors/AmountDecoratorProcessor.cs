// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Engine.Splines.Components;
using Stride.Engine.Splines.Models.Decorators;

namespace Stride.Engine.Splines.Processors.DecoratorProcessors;

public class AmountDecoratorProcessor : BaseSplineDecoratorProcessor
{
    /// <summary>
    /// Decorates the given spline from the Spline component with a fixed amount of decorations, which are evenly distributed along the spline
    /// </summary>
    public override void Decorate(SplineDecoratorComponent component)
    {
        if (component.Entity == null
            || component.SplineComponent?.Spline == null
            || component.SplineComponent.Spline.TotalSplineDistance <= 0
            || component.DecoratorSettings.Decorations.Count == 0
            || component.DecoratorSettings is not SplineAmountDecoratorSettings amountDecorator || amountDecorator.Amount <= 0)
        {
            return;
        }

        var random = new Random();
        var percentagePerInstance = 100 / (amountDecorator.Amount-1);
        for (int iteration = 0; iteration < amountDecorator.Amount; iteration++)
        {
            CreateInstance(component, iteration, percentagePerInstance * iteration, random);
        }
    }
}

// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Engine.Splines.Components;
using Stride.Engine.Splines.Models.Decorators;

namespace Stride.Engine.Splines.Processors.DecoratorProcessors;

public abstract class BaseSplineDecoratorProcessor
{
    public abstract void Decorate(SplineDecoratorComponent component);

    /// <summary>
    /// Creates a new instance from the decorations pool, and adds it to the scene.
    /// </summary>
    /// <param name="component"></param>
    /// <param name="iteration"></param>
    /// <param name="percentage"></param>
    protected void CreateInstance(SplineDecoratorComponent component, int iteration, float percentage, Random random)
    {
        var splinePositionInfo = component.SplineComponent.Spline.GetPositionOnSpline(percentage);
        var instanceRoot = new Entity("Spline" + iteration);

        var count = component.DecoratorSettings.Decorations.Count;
        var prefabToInstantiate = component.DecoratorSettings.SpawnOrder == SplineDecoratorInstanceEnum.Sequential 
            ? component.DecoratorSettings.Decorations[iteration % count] 
            : component.DecoratorSettings.Decorations[random.Next(0, count)];
        
        var instanceEntities = prefabToInstantiate.Instantiate();
        
        instanceRoot.Transform.Position = EntityTransformExtensions.WorldToLocal(component.Entity.Transform, splinePositionInfo.Position);
        instanceRoot.Transform.UpdateWorldMatrix();
        
        foreach (var instanceEntity in instanceEntities)
        {
            instanceRoot.AddChild(instanceEntity);
        }
        
        component.DecorationInstances.Add(instanceRoot);
        component.Entity.AddChild(instanceRoot);
    }
}

// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Engine.Splines.Models.Decorators;

[DataContract("SplineIntervalDecorator")]
[Display("Interval")]
public class SplineIntervalDecoratorSettings : SplineDecoratorSettings
{
    /// <summary>
    /// The random distribution of instantiated prefabs along a spline <see cref="SplineIntervalDecoratorSettings"/>
    /// </summary>
    /// <userdoc>
    /// Each instance of a decorator prefab is placed alongside the spline, as long as the spline length is not covered,
    /// new instances will be made for each random length interval
    /// 
    /// </userdoc>
    [Display("Interval")]
    public Vector2 Interval { get; set; } = new Vector2(1, 1);
}

// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Engine.Splines.Models.Decorators;

[DataContract("SplineAmountDecorator")]
[Display("Amount")]
public class SplineAmountDecoratorSettings : SplineDecoratorSettings
{
    /// <summary>
    /// The fixed amount of decorations to be instantiated on a spline <see cref="SplineAmountDecoratorSettings"/>
    /// </summary>
    /// <userdoc>
    /// The fixed amount of decorations to be instantiated on a spline
    /// </userdoc>
    [Display(40, "Distance")]
    public int Amount { get; set; } = 4;
}

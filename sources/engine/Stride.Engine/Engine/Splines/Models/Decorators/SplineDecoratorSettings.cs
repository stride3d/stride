// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core;

namespace Stride.Engine.Splines.Models.Decorators;

[DataContract(Inherited = true)]
public abstract class SplineDecoratorSettings
{
    /// <summary>
    /// A list of prefabs that the decorators uses to instantiate
    /// </summary>
    [Display(20, "Decorations")] 
    public List<Prefab> Decorations = new List<Prefab>();
    
    /// <summary>
    /// The way that decorations are spawned
    /// </summary>
    [Display(30, "Spawn order")]
    public SplineDecoratorInstanceEnum SpawnOrder;
}

public enum SplineDecoratorInstanceEnum
{
    /// <summary>
    /// The prefabs in the decorations list are randomly picked for instantiation
    /// </summary>
    Random,
    
    /// <summary>
    /// The prefabs in the decorations list are picked in sequential order
    /// </summary>
    Sequential
}

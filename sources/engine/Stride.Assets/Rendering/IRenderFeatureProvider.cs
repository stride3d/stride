// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;

using Stride.Core.Reflection;
using Stride.Rendering;

namespace Stride.Assets.Rendering;

/// <summary>
///   Provides render features to a graphics compositor.
/// </summary>
/// <remarks>
///   This interface is used by loaded packages to provide render features to the graphics compositor,
///   binding them to the appropriate stages.
///   <para/>
///   Implementers of this interface are found by assembly scanning and are expected to have a parameterless constructor.
/// </remarks>
[AssemblyScan]
public interface IRenderFeatureProvider
{
    /// <summary>
    ///   Creates render features for the graphics compositor.
    /// </summary>
    /// <param name="context">Provides access to the render stages of the graphics compositor.</param>
    /// <returns>A collection of render features to be added to the compositor.</returns>
    IEnumerable<RootRenderFeature> CreateRenderFeatures(RenderFeatureProviderContext context);
}

// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core.Reflection;
using Stride.Rendering;

namespace Stride.Assets.Rendering;

/// <summary>
/// The render features a package brings to a graphics compositor, for the two stages the compositor renders
/// into. Found by scanning the loaded asset assemblies, created with the parameterless constructor: the editor's
/// preview and thumbnail compositors take them, so the package's components render there.
/// </summary>
[AssemblyScan]
public interface IRenderFeatureProvider
{
    IEnumerable<RootRenderFeature> CreateRenderFeatures(RenderStage opaqueStage, RenderStage transparentStage);
}

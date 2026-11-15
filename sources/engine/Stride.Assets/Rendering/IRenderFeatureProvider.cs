// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core.Reflection;
using Stride.Rendering;

namespace Stride.Assets.Rendering;

/// <summary>
/// The render features a package brings to a <see cref="GraphicsCompositorAsset"/>, rendering into its opaque and transparent stages.
/// Found by assembly scan and created with the parameterless constructor.
/// </summary>
[AssemblyScan]
public interface IRenderFeatureProvider
{
    IEnumerable<RootRenderFeature> CreateRenderFeatures(RenderStage opaqueStage, RenderStage transparentStage);
}

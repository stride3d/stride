// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Rendering;

/// <summary>
/// Deforms the models that a view collected this frame, after every view is culled and before any is drawn.
/// </summary>
internal interface IModelDeformer
{
    /// <summary>Deforms <paramref name="renderModel"/>, at most once per frame, when <see cref="Flush"/> is called.</summary>
    void Deform(RenderModel renderModel);

    /// <summary>Records the GPU work of the models passed to <see cref="Deform"/>.</summary>
    void Flush();
}

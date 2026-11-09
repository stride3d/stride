// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Rendering
{
    /// <summary>
    /// The kinds of view a material shader can tell apart through the <c>RenderViewKind</c> shader; the values match its
    /// <c>ViewKind*</c> constants.
    /// </summary>
    /// <remarks>
    /// <see cref="Materials.MaterialRenderFeature"/> writes the kind for the views of mesh materials only; sprites, the
    /// background, particles and UI are other root render features, where it is undefined.
    /// </remarks>
    public static class RenderViewKind
    {
        /// <summary>Any view that is not one of the kinds below (camera, prepass, VR eye, voxelization...).</summary>
        public const uint Default = 0;

        /// <summary>A shadow map view (<see cref="Shadows.ShadowMapRenderView"/>), whatever effect draws it.</summary>
        public const uint ShadowMap = 1;
    }
}

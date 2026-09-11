// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11

namespace Stride.Graphics;

public static partial class GraphicsBackend
{
    /// <inheritdoc/>
    public static partial bool Implements(GraphicsCapabilityKind kind) => true;
}

#endif

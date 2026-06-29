// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

/// <summary>Surface-to-display rotation; renderer folds into projection to skip engine compose.</summary>
internal enum SurfaceRotation
{
    Identity,
    Rotate90,
    Rotate180,
    Rotate270,
}

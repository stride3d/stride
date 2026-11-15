// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Rendering;

/// <summary>Thread group shape of compute deformation: vertices (X) by instances of the same mesh (Y).</summary>
internal enum DeformationThreadGroup
{
    /// <summary>32 vertices of a single instance.</summary>
    X32Y1,
    /// <summary>8 vertices by 16 instances.</summary>
    X8Y16,
}

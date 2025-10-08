// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using DotRecast.Core.Numerics;
using Stride.Core.Mathematics;

namespace Stride.DotRecast.Extensions;

public static class StrideDotRecastExtensions
{
    // hopefully wont be a thing in the future
    // https://github.com/ikpil/DotRecast/issues/12
    // https://github.com/ikpil/DotRecast/tree/pr/change-rcvec3-to-numerics-verctor3
    public static RcVec3f ToDotRecastVector(this Vector3 vec)
    {
        return Unsafe.As<Vector3, RcVec3f>(ref vec);
    }

    public static Vector3 ToStrideVector(this RcVec3f vec)
    {
        return Unsafe.As<RcVec3f, Vector3>(ref vec);
    }
}

using DotRecast.Core.Numerics;
using Stride.Core.Mathematics;

namespace Stride.BepuPhysics.Navigation.Extensions;
public static class StrideDotRecastExtensions
{
    // hopefully wont be a thing in the near future
    // https://github.com/ikpil/DotRecast/issues/12
    // https://github.com/ikpil/DotRecast/tree/pr/change-rcvec3-to-numerics-verctor3
    public static RcVec3f ToDotRecastVector(this Vector3 vec)
    {
        //return Unsafe.As<RcVec3f, Vector3>(ref vec);
        return new RcVec3f(vec.X, vec.Y, vec.Z);
    }
    public static Vector3 ToStrideVector(this RcVec3f vec)
    {
        //return Unsafe.As<RcVec3f, Vector3>(ref vec);
        return new Vector3(vec.X, vec.Y, vec.Z);
    }
}

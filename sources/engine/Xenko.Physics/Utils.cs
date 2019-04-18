// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Mathematics;

namespace Xenko.Physics
{
    public static class Utils
    {
        public static BulletSharp.Math.Matrix ToBullet(this Matrix value)
        {
            // 11, 22, 33 & 44's offsets are shared, ~10% faster
            BulletSharp.Math.Matrix d;
            unsafe
            {
                d = *(BulletSharp.Math.Matrix*)&value;
            }
            //11
            d.M12 = value.M12;
            d.M13 = value.M13;
            d.M14 = value.M14;
            d.M21 = value.M21;
            //22
            d.M23 = value.M23;
            d.M24 = value.M24;
            d.M31 = value.M31;
            d.M32 = value.M32;
            //33
            d.M34 = value.M34;
            d.M41 = value.M41;
            d.M42 = value.M42;
            d.M43 = value.M43;
            //44
            return d;
        }

        public static Matrix ToXenko(this BulletSharp.Math.Matrix value)
        {
            // 11, 22, 33 & 44's offsets are shared, ~10% faster
            Matrix d;
            unsafe
            {
                d = *(Matrix*)&value;
            }
            //11
            d.M12 = value.M12;
            d.M13 = value.M13;
            d.M14 = value.M14;
            d.M21 = value.M21;
            //22
            d.M23 = value.M23;
            d.M24 = value.M24;
            d.M31 = value.M31;
            d.M32 = value.M32;
            //33
            d.M34 = value.M34;
            d.M41 = value.M41;
            d.M42 = value.M42;
            d.M43 = value.M43;
            //44
            return d;
        }

        public static BulletSharp.Math.Quaternion ToBullet(this Quaternion value)
        {
            unsafe { return *(BulletSharp.Math.Quaternion*)&value; }
        }

        public static Quaternion ToXenko(this BulletSharp.Math.Quaternion value)
        {
            unsafe { return *(Quaternion*)&value; }
        }

        public static BulletSharp.Math.Vector3 ToBullet(this Vector3 value)
        {
            unsafe { return *(BulletSharp.Math.Vector3*)&value; }
        }

        public static Vector3 ToXenko(this BulletSharp.Math.Vector3 value)
        {
            unsafe { return *(Vector3*)&value; }
        }
    }
}

using System.Numerics;
using BepuPhysics;
using Stride.Engine;

namespace BepuPhysicIntegrationTest
{
    public static class Extensions
    {
        public static Vector3 ToNumericVector(this Stride.Core.Mathematics.Vector3 vec)
        {
            return new Vector3(vec.X, vec.Y, vec.Z);
        }
        public static Stride.Core.Mathematics.Vector3 ToStrideVector(this Vector3 vec)
        {
            return new Stride.Core.Mathematics.Vector3(vec.X, vec.Y, vec.Z);
        }

        public static Quaternion ToNumericQuaternion(this Stride.Core.Mathematics.Quaternion qua)
        {
            return new Quaternion(qua.X, qua.Y, qua.Z, qua.W);
        }
        public static Stride.Core.Mathematics.Quaternion ToStrideQuaternion(this Quaternion qua)
        {
            return new Stride.Core.Mathematics.Quaternion(qua.X, qua.Y, qua.Z, qua.W);
        }

        public static RigidPose ToBepuPose(this TransformComponent transform)
        {
            return new RigidPose(transform.Position.ToNumericVector(), transform.Rotation.ToNumericQuaternion());
        }

        public static T GetInMeOrParents<T>(this Entity entity) where T : EntityComponent
        {
            while (entity != null)
            {
                var res = entity.Get<T>();
                if (res != null)
                    return res;
                entity = entity.GetParent();
            }
            return null;
        }
        public static T GetInMeOrChilds<T>(this Entity entity) where T : EntityComponent
        {
            var res = entity.Get<T>();
            if (res != null)
                return res;

            var childrens = entity.GetChildren();
            foreach (var child in childrens)
            {
                res = child.GetInMeOrChilds<T>();
                if (res != null)
                    return res;
            }
            return null;
        }

        public static T GetInParents<T>(this Entity entity) where T : EntityComponent
        {
            entity = entity.GetParent();
            while (entity != null)
            {
                var res = entity.Get<T>();
                if (res != null)
                    return res;
                entity = entity.GetParent();
            }
            return null;
        }
        public static T GetInChilds<T>(this Entity entity) where T : EntityComponent
        {
            var childrens = entity.GetChildren();
            foreach (var child in childrens)
            {
                var res = child.GetInMeOrChilds<T>();
                if (res != null)
                    return res;
            }
            return null;
        }
    }
}

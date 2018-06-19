// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Particles.DebugDraw;

namespace Xenko.Particles.Updaters.FieldShapes
{
    [DataContract("FieldShape")]
    public abstract class FieldShape
    {
        public abstract DebugDrawShape GetDebugDrawShape(out Vector3 pos, out Quaternion rot, out Vector3 scl);

        public abstract void PreUpdateField(Vector3 position, Quaternion rotation, Vector3 size);

        public abstract float GetDistanceToCenter(
            Vector3 particlePosition, Vector3 particleVelocity,
            out Vector3 alongAxis, out Vector3 aroundAxis, out Vector3 awayAxis);

        public abstract bool IsPointInside(Vector3 particlePosition, out Vector3 surfacePoint, out Vector3 surfaceNormal);
    }
}

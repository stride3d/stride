// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Mathematics;

namespace Xenko.Physics
{
    internal class XenkoMotionState : BulletSharp.MotionState
    {
        private RigidbodyComponent rigidBody;

        public XenkoMotionState(RigidbodyComponent rb)
        {
            rigidBody = rb;
        }

        public void Clear()
        {
            rigidBody = null;
        }

        public override void GetWorldTransform(out BulletSharp.Math.Matrix transform)
        {
            if (rigidBody.GetWorldTransformCallback != null)
            {
                rigidBody.GetWorldTransformCallback(out Matrix temp);
                transform = temp;
            }
            else
            {
                transform = Matrix.Identity;
            }
        }

        public override void SetWorldTransform(ref BulletSharp.Math.Matrix transform)
        {
            rigidBody.SetWorldTransformCallback?.Invoke(transform);
        }
    }
}

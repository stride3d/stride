// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Mathematics;

namespace Xenko.Physics
{
    internal class XenkoMotionState : BulletSharp.SharpMotionState
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

        public override void GetWorldTransform(out Matrix transform)
        {
            if (rigidBody.GetWorldTransformCallback != null)
            {
                rigidBody.GetWorldTransformCallback(out transform);
            }
            else
            {
                transform = Matrix.Identity;
            }
        }

        public override void SetWorldTransform(Matrix transform)
        {
            rigidBody.SetWorldTransformCallback?.Invoke(transform);
        }
    }
}

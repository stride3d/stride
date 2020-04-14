// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading;

namespace Stride.Graphics
{
    /// <summary>
    /// Used to prevent concurrent uses of CommandList against the main one.
    /// </summary>
    public struct DefaultCommandListLock : IDisposable
    {
        private readonly bool lockTaken;
        private object lockObject;

        public DefaultCommandListLock(CommandList lockObject)
        {
            if (lockObject.GraphicsDevice.InternalMainCommandList == lockObject)
            {
                this.lockObject = lockObject;
                lockTaken = false;
                Monitor.Enter(lockObject, ref lockTaken);
            }
            else
            {
                this.lockTaken = false;
                this.lockObject = null;
            }
        }

        public void Dispose()
        {
            if (lockTaken)
                Monitor.Exit(lockObject);
            lockObject = null;
        }
    }
}

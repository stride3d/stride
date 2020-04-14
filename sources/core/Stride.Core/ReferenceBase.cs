// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading;

namespace Stride.Core
{
    /// <summary>
    /// Base class for a <see cref="IReferencable"/> class.
    /// </summary>
    public abstract class ReferenceBase : IReferencable
    {
        private int counter = 1;

        /// <inheritdoc/>
        public int ReferenceCount { get { return counter; } }

        /// <inheritdoc/>
        public virtual int AddReference()
        {
            var newCounter = Interlocked.Increment(ref counter);
            if (newCounter <= 1) throw new InvalidOperationException(FrameworkResources.AddReferenceError);
            return newCounter;
        }

        /// <inheritdoc/>
        public virtual int Release()
        {
            var newCounter = Interlocked.Decrement(ref counter);
            if (newCounter == 0)
            {
                try
                {
                    Destroy();
                }
                finally
                {
                    // Reverse back the counter if there are any exceptions in the destroy method
                    Interlocked.Exchange(ref counter, newCounter + 1);
                }
            }
            else if (newCounter < 0)
            {
                throw new InvalidOperationException(FrameworkResources.ReleaseReferenceError);
            }
            return newCounter;
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        protected abstract void Destroy();
    }
}

// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading;
using Stride.Core.ReferenceCounting;

namespace Stride.Core
{
    /// <summary>
    /// Base class for a <see cref="IDisposable"/> interface.
    /// </summary>
    [DataContract]
    public abstract class DisposeBase : IDisposable, IReferencable
    {
        private int counter = 1;

        public void Dispose()
        {
            if (!IsDisposed)
            {
                this.ReleaseInternal();
            }
        }

        /// <summary>
        /// Has the component been disposed or not yet.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Disposes of object resources.
        /// </summary>
        protected virtual void Destroy()
        {
        }

        /// <inheritdoc/>
        int IReferencable.ReferenceCount => counter;

        /// <inheritdoc/>
        int IReferencable.AddReference()
        {
            OnAddReference();

            var newCounter = Interlocked.Increment(ref counter);
            if (newCounter <= 1) throw new InvalidOperationException(FrameworkResources.AddReferenceError);
            return newCounter;
        }

        /// <inheritdoc/>
        int IReferencable.Release()
        {
            OnReleaseReference();

            var newCounter = Interlocked.Decrement(ref counter);
            if (newCounter == 0)
            {
                Destroy();
                IsDisposed = true;
            }
            else if (newCounter < 0)
            {
                throw new InvalidOperationException(FrameworkResources.ReleaseReferenceError);
            }
            return newCounter;
        }

        protected virtual void OnAddReference()
        {
        }

        protected virtual void OnReleaseReference()
        {
        }
    }
}

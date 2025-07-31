// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_NULL

using System;

namespace Stride.Graphics
{
    /// <summary>
    /// GraphicsResource class
    /// </summary>
    public abstract partial class GraphicsResourceBase
    {
        /// <summary>
        ///   Perform platform-specific initialization of the Graphics Resource.
        /// </summary>
        private partial void Initialize()
        {
            NullHelper.ToImplement();
        }

        /// <summary>
        ///   Called when the <see cref="GraphicsDevice"/> has been detected to be internally destroyed,
        ///   or when the <see cref="Destroy"/> methad has been called. Raises the <see cref="Destroyed"/> event.
        /// </summary>
        protected internal virtual partial void OnDestroyed()
        {
            Destroyed?.Invoke(this, EventArgs.Empty);
            NullHelper.ToImplement();
        }

        /// <summary>
        /// Called when graphics device has been recreated.
        /// </summary>
        /// <returns>True if item transitioned to a <see cref="GraphicsResourceLifetimeState.Active"/> state.</returns>
        protected internal virtual bool OnRecreate()
        {
            NullHelper.ToImplement();
            return false;
        }
    }
}
#endif

// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_OPENGL 
using System;

namespace Stride.Graphics
{
    /// <summary>
    /// GraphicsResource class
    /// </summary>
    public partial class GraphicsResourceBase
    {
        private void Initialize()
        {
        }
        
        /// <summary>
        /// Called when graphics device has been detected to be internally destroyed.
        /// </summary>
        /// <inheritdoc/>
        protected internal virtual void OnDestroyed()
        {
            Destroyed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Called when graphics device has been recreated.
        /// </summary>
        /// <returns>True if item transitioned to a <see cref="GraphicsResourceLifetimeState.Active"/> state.</returns>
        protected internal virtual bool OnRecreate()
        {
            return false;
        }
    }
}
 
#endif

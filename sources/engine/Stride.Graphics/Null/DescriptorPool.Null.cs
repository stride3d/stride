// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_NULL

namespace Stride.Graphics
{
    public partial class DescriptorPool
    {

        /// <summary>
        /// Initializes new instance of <see cref="DescriptorPool"/> that can handle the various 
        /// <see cref="DescriptorTypeCount"/> from <param name="counts"/> for <param name="graphicsDevice"/>.
        /// </summary>
        /// <param name="graphicsDevice">The graphics device.</param>
        /// <param name="counts">Various Type and corresponding count for descriptors that this instance will handle.</param>
        private DescriptorPool(GraphicsDevice graphicsDevice, DescriptorTypeCount[] counts) : base(graphicsDevice)
        {
            NullHelper.ToImplement();
        }

        /// <summary>
        /// Reset the pool.
        /// </summary>
        public void Reset()
        {
            NullHelper.ToImplement();
        }

        /// <summary>
        /// Recreate the pool.
        /// </summary>
        private void Recreate()
        {
            NullHelper.ToImplement();
        }
    }
}

#endif

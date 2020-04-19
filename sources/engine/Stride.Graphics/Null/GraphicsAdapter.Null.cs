// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_NULL 

namespace Stride.Graphics
{
    /// <summary>
    /// Provides methods to retrieve and manipulate graphics adapters.
    /// </summary>
    public partial class GraphicsAdapter
    {
        /// <summary>
        /// Gets the description of this adapter.
        /// </summary>
        /// <value>The description.</value>
        public string Description
        {
            get
            {
                NullHelper.ToImplement();
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets or sets the vendor identifier.
        /// </summary>
        /// <value>
        /// The vendor identifier.
        /// </value>
        public int VendorId
        {
            get
            {
                NullHelper.ToImplement();
                return 0;
            }
        }

        /// <summary>
        /// Determines if this instance of GraphicsAdapter is the default adapter.
        /// </summary>
        public bool IsDefaultAdapter
        {
            get
            {
                NullHelper.ToImplement();
                return true;
            }
        }

        /// <summary>
        /// Tests to see if the adapter supports the requested profile.
        /// </summary>
        /// <param name="graphicsProfile">The graphics profile.</param>
        /// <returns>true if the profile is supported</returns>
        public bool IsProfileSupported(GraphicsProfile graphicsProfile)
        {
            NullHelper.ToImplement();
            return false;
        }
    }
} 
#endif

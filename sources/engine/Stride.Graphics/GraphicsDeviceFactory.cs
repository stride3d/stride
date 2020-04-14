// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Graphics
{
    /// <summary>
    /// Factory for <see cref="GraphicsDevice"/>.
    /// </summary>
    /*public abstract class GraphicsDeviceFactory : ComponentBase
    {
        private GraphicsAdapterFactory AdapterFactory { get; set; }

        internal GraphicsDeviceFactory(GraphicsAdapterFactory adapterFactory)
        {
            AdapterFactory = adapterFactory;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphicsDevice"/> class using the default GraphicsAdapter
        /// and the Level10 <see cref="GraphicsProfile"/>.
        /// </summary>
        /// <returns>An instance of <see cref="GraphicsDevice"/></returns>
        public GraphicsDevice New()
        {
            return New(GraphicsProfile.Level10);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphicsDevice"/> class using the default GraphicsAdapter.
        /// </summary>
        /// <param name="graphicsProfile">The graphics profile.</param>
        /// <returns>An instance of <see cref="GraphicsDevice"/></returns>
        public GraphicsDevice New(GraphicsProfile graphicsProfile)            
        {
            return New(null, graphicsProfile);
        }
            
        /// <summary>
        /// Initializes a new instance of the <see cref="GraphicsDevice"/> class using the Level10 <see cref="GraphicsProfile"/>.
        /// </summary>
        /// <param name="adapter">The GraphicsAdapter to use with this graphics device.</param>
        /// <returns>An instance of <see cref="GraphicsDevice"/></returns>
        public GraphicsDevice New(GraphicsAdapter adapter)
        {
            return New(adapter, GraphicsProfile.Level10);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphicsDevice"/> class.
        /// </summary>
        /// <param name="adapter">The GraphicsAdapter to use with this graphics device.</param>
        /// <param name="graphicsProfile">The graphics profile.</param>
        /// <returns>An instance of <see cref="GraphicsDevice"/></returns>
        public GraphicsDevice New(GraphicsAdapter adapter, GraphicsProfile graphicsProfile)
        {
            return New(adapter ?? AdapterFactory.Default, graphicsProfile, null);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphicsDevice"/> class.
        /// </summary>
        /// <param name="graphicsProfile">The graphics profile.</param>
        /// <param name="presentationParameters">The presentation parameters.</param>
        /// <returns>An instance of <see cref="GraphicsDevice"/></returns>
        public GraphicsDevice New(GraphicsProfile graphicsProfile,
                                           PresentationParameters presentationParameters)
        {
            return New(AdapterFactory.Default, graphicsProfile, presentationParameters);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphicsDevice"/> class.
        /// </summary>
        /// <param name="adapter">The GraphicsAdapter to use with this graphics device. null is for default.</param>
        /// <param name="graphicsProfile">The graphics profile.</param>
        /// <param name="presentationParameters">The presentation parameters.</param>
        /// <returns>An instance of <see cref="GraphicsDevice"/></returns>
        public abstract GraphicsDevice New(GraphicsAdapter adapter, GraphicsProfile graphicsProfile,
                                           PresentationParameters presentationParameters);
    }*/
}

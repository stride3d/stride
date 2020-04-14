// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;

namespace Stride.Graphics
{
    /// <summary>
    /// Root factory for all Graphics components.
    /// </summary>
    public class GraphicsFactory : ComponentBase
    {
        /// <summary>
        /// GraphicsApi key.
        /// TODO not the best place to store this identifier. Move it to GraphicsFactory?
        /// </summary>
        public const string GraphicsApi = "GraphicsApi";
    }
}

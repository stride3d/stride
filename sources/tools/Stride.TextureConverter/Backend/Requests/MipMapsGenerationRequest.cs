// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stride.TextureConverter.Requests
{
    /// <summary>
    /// Request to generate the mipmap chain on a texture (3d texture mipmap generation not yet supported)
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="MipMapsGenerationRequest"/> class.
    /// </remarks>
    /// <param name="filter">The filter.</param>
    internal class MipMapsGenerationRequest(Filter.MipMapGeneration filter) : IRequest
    {
        public override RequestType Type { get { return RequestType.MipMapsGeneration; } }


        /// <summary>
        /// The filter to be used when rescaling to create the mipmaps of lower level.
        /// </summary>
        /// <value>
        /// The filter.
        /// </value>
        public Filter.MipMapGeneration Filter { get; private set; } = filter;
    }
}

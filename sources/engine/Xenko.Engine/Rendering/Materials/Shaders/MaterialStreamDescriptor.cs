// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Xenko.Core.Mathematics;
using Xenko.Shaders;

namespace Xenko.Rendering.Materials
{
    /// <summary>
    /// A Material Stream.
    /// </summary>
    public class MaterialStreamDescriptor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialStreamDescriptor"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="stream">The stream.</param>
        /// <param name="colorType">Type of the color.</param>
        public MaterialStreamDescriptor(string name, string stream, Type colorType, bool remapSigned = false)
            : this(name, stream, GetDefaultFilter(stream, remapSigned, colorType))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialStreamDescriptor"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="stream">The stream.</param>
        /// <param name="filter">The filter.</param>
        /// <exception cref="System.ArgumentNullException">
        /// name
        /// or
        /// stream
        /// </exception>
        public MaterialStreamDescriptor(string name, string stream, ShaderSource filter = null)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (stream == null) throw new ArgumentNullException("stream");
            Name = name;
            Stream = stream;
            Filter = filter ?? GetDefaultFilter(stream, false, typeof(Color3));
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the stream name.
        /// </summary>
        /// <value>The stream.</value>
        public string Stream { get; private set; }

        /// <summary>
        /// Gets the filter to modify the shader.
        /// </summary>
        /// <value>The filter.</value>
        public ShaderSource Filter { get; private set; }

        private static ShaderSource GetDefaultFilter(string streamName, bool remapSigned, Type colorType)
        {
            string colorChannel = "rrr";
            if (colorType == typeof(Vector3) || colorType == typeof(Color3) || colorType == typeof(Vector2) || colorType == typeof(Color4) || colorType == typeof(Vector4) || colorType == typeof(Color))
            {
                colorChannel = "rgb";
            }
            return new ShaderClassSource("MaterialSurfaceStreamShading", streamName, colorChannel, remapSigned);
        }
    }
}

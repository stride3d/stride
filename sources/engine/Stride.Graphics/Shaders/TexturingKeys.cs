// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.Rendering
{
    public partial class TexturingKeys
    {
        static TexturingKeys()
        {
            DefaultTextures = new ReadOnlyCollection<ObjectParameterKey<Texture>>(new List<ObjectParameterKey<Texture>>()
            {
                Texture0,
                Texture1,
                Texture2,
                Texture3,
                Texture4,
                Texture5,
                Texture6,
                Texture7,
                Texture8,
                Texture9,
            });
            TextureCubes = new ReadOnlyCollection<ObjectParameterKey<Texture>>(new List<ObjectParameterKey<Texture>>()
            {
                TextureCube0,
                TextureCube1,
                TextureCube2,
                TextureCube3,
            });
            Textures3D = new ReadOnlyCollection<ObjectParameterKey<Texture>>(new List<ObjectParameterKey<Texture>>()
            {
                Texture3D0,
                Texture3D1,
                Texture3D2,
                Texture3D3,
            });

            TexturesTexelSize = new ReadOnlyCollection<ValueParameterKey<Vector2>>(new List<ValueParameterKey<Vector2>>()
            {
                Texture0TexelSize,
                Texture1TexelSize,
                Texture2TexelSize,
                Texture3TexelSize,
                Texture4TexelSize,
                Texture5TexelSize,
                Texture6TexelSize,
                Texture7TexelSize,
                Texture8TexelSize,
                Texture9TexelSize,
            });
        }

        /// <summary>
        /// Default textures used by this class (<see cref="Texture0"/>, <see cref="Texture1"/>...etc.)
        /// </summary>
        public static readonly IReadOnlyList<ObjectParameterKey<Texture>> DefaultTextures;

        /// <summary>
        /// The cube textures used by this class (<see cref="TextureCube0"/>, <see cref="TextureCube1"/>...etc.)
        /// </summary>
        public static readonly IReadOnlyList<ObjectParameterKey<Texture>> TextureCubes;

        /// <summary>
        /// The 3d textures used by this class (<see cref="Texture3D0"/>, <see cref="Texture3D1"/>...etc.)
        /// </summary>
        public static readonly IReadOnlyList<ObjectParameterKey<Texture>> Textures3D;

        /// <summary>
        /// Default textures size used by this class (<see cref="Texture0TexelSize"/>, <see cref="Texture1TexelSize"/>...etc.)
        /// </summary>
        public static readonly IReadOnlyList<ValueParameterKey<Vector2>> TexturesTexelSize;
    }
}

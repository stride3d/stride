// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;

using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.Rendering;

/// <summary>
///   Common keys used for texturing in Stride rendering.
/// </summary>
public partial class TexturingKeys
{
    static TexturingKeys()
    {
        DefaultTextures = new ReadOnlyCollection<ObjectParameterKey<Texture>>(
        [
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
        ]);
        TextureCubes = new ReadOnlyCollection<ObjectParameterKey<Texture>>(
        [
            TextureCube0,
            TextureCube1,
            TextureCube2,
            TextureCube3,
        ]);
        Textures3D = new ReadOnlyCollection<ObjectParameterKey<Texture>>(
        [
            Texture3D0,
            Texture3D1,
            Texture3D2,
            Texture3D3,
        ]);

        TexturesTexelSize = new ReadOnlyCollection<ValueParameterKey<Vector2>>(
        [
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
        ]);
    }


    /// <summary>
    ///   Parameter keys for the default Textures (<see cref="Texture0"/>, <see cref="Texture1"/>...etc.)
    /// </summary>
    public static readonly IReadOnlyList<ObjectParameterKey<Texture>> DefaultTextures;

    /// <summary>
    ///   Parameter keys for the Cube Textures (<see cref="TextureCube0"/>, <see cref="TextureCube1"/>...etc.)
    /// </summary>
    public static readonly IReadOnlyList<ObjectParameterKey<Texture>> TextureCubes;

    /// <summary>
    ///   Parameter keys for the 3D Textures (<see cref="Texture3D0"/>, <see cref="Texture3D1"/>...etc.)
    /// </summary>
    public static readonly IReadOnlyList<ObjectParameterKey<Texture>> Textures3D;

    /// <summary>
    ///   Parameter keys for the texel size for the default Textures (<see cref="Texture0TexelSize"/>, <see cref="Texture1TexelSize"/>...etc.)
    /// </summary>
    /// <remarks>
    ///   The <strong>texel size</strong> is a vector that contains the width and height of a pixel of a Texture in UV space,
    ///   i.e. <c>(1.0 / sizeOfTheTexture)</c>.
    /// </remarks>
    public static readonly IReadOnlyList<ValueParameterKey<Vector2>> TexturesTexelSize;
}

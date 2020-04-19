// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Shaders;

namespace Stride.Graphics
{
    internal partial class SpriteSignedDistanceFieldFontShader
    {
        private static EffectBytecode bytecode;

        internal static EffectBytecode Bytecode
        {
            get
            {
                return bytecode ?? (bytecode = EffectBytecode.FromBytesSafe(spriteSignedDistanceFieldFontBytecode));
            }
        }
    }
}

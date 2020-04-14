// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Shaders;

namespace Xenko.Graphics
{
    internal partial class SignedDistanceFieldFontShader
    {
        private static EffectBytecode bytecode;

        internal static EffectBytecode Bytecode
        {
            get
            {
                return bytecode ?? (bytecode = EffectBytecode.FromBytesSafe(signedDistanceFieldFontBytecode));
            }
        }
    }
}

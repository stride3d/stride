// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Xenko.Particles
{
    public static class ParticleUtilities
    {
        public static int AlignedSize(int size, int alignment)
        {
            return (size % alignment == 0) ? size : (size + alignment - (size % alignment));
        }
    }
}


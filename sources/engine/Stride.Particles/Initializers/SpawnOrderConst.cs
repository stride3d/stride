// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Particles.Initializers
{
    /// <summary>
    /// Spawn order can be additionally subdivided in groups
    /// </summary>
    public static class SpawnOrderConst
    {
        public const int GroupBitOffset = 16;
        public const uint GroupBitMask = 0xFFFF0000;
        public const uint AuxiliaryBitMask = 0x0000FFFF;
        public const int LargeGroupBitOffset = 20;
        public const uint LargeGroupBitMask = 0xFFF00000;
        public const uint LargeAuxiliaryBitMask = 0x000FFFFF;
    }
}

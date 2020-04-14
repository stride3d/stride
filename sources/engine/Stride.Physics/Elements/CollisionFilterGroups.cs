// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Physics
{
    public enum CollisionFilterGroups //needed for the editor as this is not tagged as flag...
    {
        DefaultFilter = 0x1,

        StaticFilter = 0x2,

        KinematicFilter = 0x4,

        DebrisFilter = 0x8,

        SensorTrigger = 0x10,

        CharacterFilter = 0x20,

        CustomFilter1 = 0x40,

        CustomFilter2 = 0x80,

        CustomFilter3 = 0x100,

        CustomFilter4 = 0x200,

        CustomFilter5 = 0x400,

        CustomFilter6 = 0x800,

        CustomFilter7 = 0x1000,

        CustomFilter8 = 0x2000,

        CustomFilter9 = 0x4000,

        CustomFilter10 = 0x8000,

        AllFilter = 0xFFFF,
    }

    [Flags]
    public enum CollisionFilterGroupFlags
    {
        DefaultFilter = 0x1,

        StaticFilter = 0x2,

        KinematicFilter = 0x4,

        DebrisFilter = 0x8,

        SensorTrigger = 0x10,

        CharacterFilter = 0x20,

        CustomFilter1 = 0x40,

        CustomFilter2 = 0x80,

        CustomFilter3 = 0x100,

        CustomFilter4 = 0x200,

        CustomFilter5 = 0x400,

        CustomFilter6 = 0x800,

        CustomFilter7 = 0x1000,

        CustomFilter8 = 0x2000,

        CustomFilter9 = 0x4000,

        CustomFilter10 = 0x8000,

        AllFilter = 0xFFFF,
    }
}

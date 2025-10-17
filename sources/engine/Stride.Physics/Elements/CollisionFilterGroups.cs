// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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

    /// <summary>
    /// Flags that allows to modify default execution and performance of ray tests algorithms.
    /// </summary>
    [Flags]
    public enum EFlags : uint
    {
        None = 0,
        /// <summary>
        /// Default execution with no modifiers
        /// </summary>
        /// <summary>
        /// Do not return a hit when a ray traverses a back-facing triangle. The option refers only to triangle meshes - collision shapes are volumes and do not utilize backfaces, so it's hitpoint detection is not affected
        /// </summary>
        FilterBackfaces = 1 << 0,
        /// <summary>
        /// Do not return a hit when a ray traverses a back-facing triangle. 
/// The option refers only to triangle meshes - collision shapes are volumes and do not utilize backfaces, so it's hitpoint detection is not affected.
        /// </summary>
        KeepUnflippedNormal = 1 << 1,
        /// <summary>
        ///Prevents returned face normal getting flipped when a ray hits a back-facing triangle. 
///The option refers only to triangle meshes - collision shapes are volumes and do not utilize backfaces, so it's hitpoint detection is not affected.
        /// </summary>
        UseSubSimplexConvexCastRaytest = 1 << 2,
        /// <summary>
        /// Uses an approximate but faster ray intersection algorithm. The algorithm is also used by default, even if the flag value is not used.
        /// </summary>
        UseGjkConvexCastRaytest = 1 << 3,
        /// <summary>
        /// Switch convex cast algorithm from sub-simplex to slower, but more precise and complete Gilbert-Johnson-Keerthi variant.
        /// </summary>
        DisableHeightfieldAccelerator  = 1 << 4
        /// <summary>
        /// Don't use the heightfield raycast accelerator. This option reduces additional memory allocation at the expense of increased. CPU cycles.
        /// </summary>
    }
}

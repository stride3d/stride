// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;

namespace Stride.Physics
{
    public static class PhysicsProfilingKeys
    {
        public static ProfilingKey SimulationProfilingKey = new ProfilingKey("Physics Simulation");

        public static ProfilingKey ContactsProfilingKey = new ProfilingKey("Physics Contacts");

        public static ProfilingKey CharactersProfilingKey = new ProfilingKey(SimulationProfilingKey, "Physics Characters");
    }
}

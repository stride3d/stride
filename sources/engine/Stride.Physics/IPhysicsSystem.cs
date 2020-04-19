// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Games;

namespace Stride.Physics
{
    public interface IPhysicsSystem : IGameSystemBase
    {
        Simulation Create(PhysicsProcessor processor, PhysicsEngineFlags flags = PhysicsEngineFlags.None);
        void Release(PhysicsProcessor processor);
    }
}

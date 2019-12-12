// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Physics;

namespace Xenko.Navigation
{
    public class StaticColliderCacheSettings
    {
        public StaticColliderComponent Component { get; private set; }

        public bool AlwaysUpdateDynamicShape = true;

        public StaticColliderCacheSettings(StaticColliderComponent staticCollider)
        {
            Component = staticCollider;
        }
    }
}

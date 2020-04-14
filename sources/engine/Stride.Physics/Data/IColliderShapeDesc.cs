// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Physics
{
    public interface IColliderShapeDesc
    {
        bool Match(object obj);
        ColliderShape CreateShape();
    }

    public interface IAssetColliderShapeDesc : IColliderShapeDesc
    {
    }

    public interface IInlineColliderShapeDesc : IAssetColliderShapeDesc
    {
    }
}

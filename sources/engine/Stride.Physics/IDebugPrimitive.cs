// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Xenko.Rendering;

namespace Xenko.Physics
{
    public interface IDebugPrimitive
    {
        IEnumerable<MeshDraw> GetMeshDraws();
    }
}

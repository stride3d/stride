// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using Stride.Rendering;

namespace Stride.Physics
{
    public class DebugPrimitive : IDebugPrimitive, IEnumerable<MeshDraw>
    {
        public readonly List<MeshDraw> MeshDraws = new List<MeshDraw>();

        public void Add(MeshDraw meshDraw)
        {
            MeshDraws.Add(meshDraw);
        }

        public IEnumerable<MeshDraw> GetMeshDraws()
        {
            return MeshDraws;
        }

        public IEnumerator<MeshDraw> GetEnumerator()
        {
            return MeshDraws.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.Importer.Common
{
    public class NodeInfo
    {
        public string Name;
        public int Depth;
        public bool Preserve;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
    }
}

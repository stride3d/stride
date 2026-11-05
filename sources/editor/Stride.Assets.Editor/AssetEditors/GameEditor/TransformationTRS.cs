// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable

using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.Assets.Editor.AssetEditors.GameEditor
{
    public struct TransformationTRS
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;

        public TransformationTRS(TransformComponent transform)
        {
            Position = transform.Position;
            Rotation = transform.Rotation;
            Scale = transform.Scale;
        }
    }
}

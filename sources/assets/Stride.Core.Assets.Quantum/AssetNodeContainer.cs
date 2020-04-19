// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Core.Quantum;
using IReference = Stride.Core.Serialization.Contents.IReference;

namespace Stride.Core.Assets.Quantum
{
    public class AssetNodeContainer : NodeContainer
    {
        public AssetNodeContainer()
        {
            NodeBuilder.RegisterPrimitiveType(typeof(IReference));
            NodeBuilder.RegisterPrimitiveType(typeof(PropertyKey));
            NodeBuilder.RegisterPrimitiveType(typeof(TimeSpan));
            NodeBuilder.RegisterPrimitiveType(typeof(Guid));
            NodeBuilder.RegisterPrimitiveType(typeof(AssetId));
            NodeBuilder.RegisterPrimitiveType(typeof(Color));
            NodeBuilder.RegisterPrimitiveType(typeof(Color3));
            NodeBuilder.RegisterPrimitiveType(typeof(Color4));
            NodeBuilder.RegisterPrimitiveType(typeof(Vector2));
            NodeBuilder.RegisterPrimitiveType(typeof(Vector3));
            NodeBuilder.RegisterPrimitiveType(typeof(Vector4));
            NodeBuilder.RegisterPrimitiveType(typeof(Int2));
            NodeBuilder.RegisterPrimitiveType(typeof(Int3));
            NodeBuilder.RegisterPrimitiveType(typeof(Int4));
            NodeBuilder.RegisterPrimitiveType(typeof(Quaternion));
            NodeBuilder.RegisterPrimitiveType(typeof(RectangleF));
            NodeBuilder.RegisterPrimitiveType(typeof(Rectangle));
            NodeBuilder.RegisterPrimitiveType(typeof(Matrix));
            NodeBuilder.RegisterPrimitiveType(typeof(UPath));
            NodeBuilder.RegisterPrimitiveType(typeof(AngleSingle));
            // Register content types as primitive so they are not processed by Quantum
            foreach (var contentType in AssetRegistry.GetContentTypes())
            {
                NodeBuilder.RegisterPrimitiveType(contentType);
            }
        }
    }
}

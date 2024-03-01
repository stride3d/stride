// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.Rendering
{
    // Need to add support for fields in auto data converter
    [DataContract]
    public class MeshDraw
    {
        public byte[] VertexData;

        public VertexDeclaration VertexDeclaration;

        public PrimitiveType PrimitiveType;

        public int DrawCount;  

        public int StartLocation;

        public VertexBufferBinding[] VertexBuffers;

        public IndexBufferBinding IndexBuffer;

        public List<Vector3> VCLIST = new List<Vector3>();

        public List<Tuple<int, int, Vector3>> VCPOLYIN = new List<Tuple<int, int, Vector3>>();

        public int VertexCount { get { return VCLIST.Count; } }

        public void AV(float X, float Y, float Z)
        {
            VCLIST.Add(new Vector3(X, Y, Z));
        }
    }
}

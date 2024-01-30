// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using SharpFont;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Rendering.Materials;
using Vortice.Vulkan;

namespace Stride.Rendering
{
    [DataContract]
    public class Mesh
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Mesh"/> class.
        /// </summary>
        public Mesh()
        {
            Parameters = new ParameterCollection();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Mesh"/> class using a shallow copy constructor.
        /// </summary>
        /// <param name="mesh">The mesh.</param>
        public Mesh(Mesh mesh)
        {
            if (mesh == null) throw new ArgumentNullException("mesh");
            Draw = mesh.Draw;
            Parameters = mesh.Parameters;
            MaterialIndex = mesh.MaterialIndex;
            NodeIndex = mesh.NodeIndex;
            Name = mesh.Name;
            BoundingBox = mesh.BoundingBox;
            BoundingSphere = mesh.BoundingSphere;
            Skinning = mesh.Skinning;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Mesh" /> class.
        /// </summary>
        /// <param name="meshDraw">The mesh draw.</param>
        /// <param name="parameters">The parameters.</param>
        /// <exception cref="System.ArgumentNullException">parameters</exception>
        public Mesh(MeshDraw meshDraw, ParameterCollection parameters)
        {
            if (meshDraw == null) throw new ArgumentNullException(nameof(meshDraw));
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            Draw = meshDraw;
            Parameters = parameters;
        }

        public MeshDraw Draw { get; set; }

        public int MaterialIndex { get; set; }

        public ParameterCollection Parameters { get; }

        /// <summary>
        /// Index of the transformation node in <see cref="Model"/>.
        /// </summary>
        public int NodeIndex;

        public string Name;

        /// <summary>
        /// Gets or sets the bounding box encompassing this <see cref="Mesh"/>.
        /// </summary>
        public BoundingBox BoundingBox;

        /// <summary>
        /// Gets the bounding sphere
        /// </summary>
        public BoundingSphere BoundingSphere;

        // TODO: Skinning could be shared between multiple Mesh inside a ModelView (multimaterial, etc...)
        public MeshSkinningDefinition Skinning;

        // public List<Shape> Shapes { get; set; }

        public Dictionary<Shape, float> Shapes { get; set; }

        public Dictionary<Shape, float> GetBlendShapeWeights()
        {
            return Shapes;
        }

        public int GetBlendShapesCount()
        {
            return Shapes?.Count ?? 0;
        }

        public List<Shape> GetBlendShapeList()
        {
            return Shapes?.Keys?.ToList();
        }

        //public List<float> GetBlendShapeWeights()
        //{
        //    return Shapes?.Values?.ToList();
        //}

        public void SetBlendShapeWeightByName(string shapeName, float weight)
        {
            var shape = Shapes?.Keys?.Where(c => c.Name == shapeName).FirstOrDefault();
        }


        private void SetBlendShapeWeightByIndex(int index, float weight)
        {
            var shape = Shapes?.ElementAt(index);
        }

        void SetBendShapeWeight(Shape shape, float weight)
        {
            if (shape == null) { return; }
            bool containsShape = Shapes != null && Shapes.ContainsKey(shape);
            if (containsShape)
            {
                Shapes[shape] = weight;
            }
        }

        public Vector2[] BlendShapeWeights { get; set; }

        public Vector3[] BlendShapeVertices { get; set; }

        
        public void AddBlendShapes(Shape shape, float weight)
        {
            (Shapes ??= new()).Add(shape, weight);
            var vecArray = AdjustToDrawcoordinate(shape.Position, Draw);
            BlendShapeVertices = AdjustPositionToDrawInstance(vecArray, Draw);
            BlendShapeWeights = GetBlendWeights();
            Parameters.Set(MaterialKeys.HasBlendShape, true);
        }

        public Vector2[] GetBlendWeights()
        {
            Vector2[] shapeWeights = new Vector2[Shapes.Count];
            float cummulativeWeight = 0f;
             
            for (int i = 0; i < Shapes.Count; i++)
            {
                var shapeKV = Shapes.ElementAt(i);
                var shape = shapeKV.Key;
                var shapeWeight = Math.Clamp(shapeKV.Value, 0f, 1f);
                float adjustedWeight = 0f;
                if (cummulativeWeight >= 1f) { adjustedWeight = 0f; }
                else if (cummulativeWeight + shapeWeight > 1) { adjustedWeight = 1 - cummulativeWeight; }
                else
                {
                    adjustedWeight = shapeWeight;
                }

                cummulativeWeight += adjustedWeight;

                shapeWeights[i] = new Vector2(adjustedWeight, Math.Clamp(1 - cummulativeWeight, 0f, 1f));


            }
            return shapeWeights;
        }


        public Vector3[] AdjustToDrawcoordinate(Vec4[] positions, MeshDraw draw)
        {
            Vector3[] NewVectices = new Vector3[positions.Length];

            for (var i = 0; i < positions.Length; i++)
            {
                float x = -1 * positions[i].x;
                float y = positions[i].y;
                float z= -1*positions[i].z;    
                NewVectices[i]=new Vector3(x,y, z);
            }


            return NewVectices;
        }

        public Vector3[] AdjustPositionToDrawInstance(Vector3[] positions, MeshDraw draw)
        {
            List<int> originalVerticesIDS = draw.VCPOLYIN.Select(c => c.Item2).ToList();
            List<int> updatedVertexMapping = draw.VertexMapping.ToList();

            
            Vector3[] NewVectices = new Vector3[positions.Length];

            for (var i = 0; i < updatedVertexMapping.Count; i++)
            {
                var v= positions[originalVerticesIDS[updatedVertexMapping[i]]];
                float x = v.X; float y=v.Y; float z=v.Z;
                Vector3 vec3=new Vector3(x,y,z);
                NewVectices[i]=vec3;
            }
            return NewVectices;
        }


    }

}

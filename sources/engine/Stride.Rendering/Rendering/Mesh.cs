// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using SharpFont;
using Stride.Core;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Rendering.Materials;
using Vortice.Vulkan;
using Matrix = Stride.Core.Mathematics.Matrix;

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

        public float[] GetBlendShapeWeights()
        {
            return Shapes?.Values?.ToArray();
        }

        public int GetBlendShapesCount()
        {
            return Shapes?.Count ?? 0;
        }

        public string[] GetBlendShapeList()
        {
            return Shapes?.Keys?.Select(c=>c.Name).ToArray();
        }


        public void SetBlendShapeWeightByName(string shapeName, float weight)
        {
            var shape = Shapes?.Keys?.Where(c => c.Name == shapeName).FirstOrDefault();
            if (shape != null)
            {
                SetBendShapeWeight(shape, weight);
            }
        }


        public void SetBlendShapeWeightByIndex(int index, float weight)
        {
            var shape = Shapes?.ElementAt(index);
            if (shape != null)
            {
                SetBendShapeWeight(shape.Value.Key, weight);
            } 
        }

        void SetBendShapeWeight(Shape shape, float weight)
        {
            if (shape == null) { return; }
            bool containsShape = Shapes != null && Shapes.ContainsKey(shape);
            if (containsShape)
            {
                Shapes[shape] = weight;

                if (MATBSHAPE != null)
                {
                    int index=Shapes.IndexOf(c => c.Key == shape);
                    for (int i = 0; i < Draw.VertexMapping.Length; i++)
                    {
                        var shiftedIndex = index * Draw.VertexMapping.Length + i;
                        MATBSHAPE[shiftedIndex][3] = weight;
                    }
                }
            }   
        }

        public bool BlendShapeProcessingNecessary = false;

       public Matrix[] MATBSHAPE { get; set; }

       public float BasisKeyWeight { get; set; }

       public void ProcessBlendShapes()
       {
            if (BlendShapeProcessingNecessary)
            {
                if (Shapes == null || Shapes.Count < 1) { return; }
                var blendShapeVertices = AdjustPositionToDrawInstance();
                var blendShapesWeights = GetBlendWeights(out float cummulativeWeight); ;
                BasisKeyWeight = 1 - cummulativeWeight;

                MATBSHAPE = new Matrix[Draw.VertexMapping.Length*blendShapesWeights.Length];
                for (int iBendShape = 0; iBendShape < blendShapesWeights.Length; iBendShape++)
                {
                    for (int iVert = 0; iVert < Draw.VertexMapping.Length; iVert++)
                    {
                        var vectorValue = blendShapeVertices.ElementAt(iBendShape * Draw.VertexMapping.Length + iVert);
                        MATBSHAPE[iBendShape * Draw.VertexMapping.Length + iVert].Row1 = new Vector4(vectorValue, blendShapesWeights[iBendShape]);

                    }
                }
                BlendShapeProcessingNecessary = false;
            }
        }
        

        public void AddBlendShapes(Shape shape, float weight)
        {
            (Shapes ??= new()).Add(shape, weight); 
            Parameters.Set(MaterialKeys.HasBlendShape, true);
            
        }

        public float[] GetBlendWeights(out float cummulativeWeight)
        {
            float[] shapeWeights = new float[Shapes.Count];
            cummulativeWeight = 0f;
             
            for (int i = 0; i < Shapes.Count; i++)
            {
                var shapeKV = Shapes.ElementAt(i);
                var shape = shapeKV.Key;
                var shapeWeight = Math.Clamp(shapeKV.Value, 0f, 1f);
                float adjustedWeight = shapeWeight;
               // if (cummulativeWeight >= 1f) { adjustedWeight = 0f; }
               // else if (cummulativeWeight + shapeWeight > 1) { adjustedWeight = 1 - cummulativeWeight; }
                //else
                //{
                  //  adjustedWeight = shapeWeight;
                //}
                cummulativeWeight += adjustedWeight;
                shapeWeights[i] = adjustedWeight;
            }
            return shapeWeights;
        }


  
        public List<Vector3> AdjustPositionToDrawInstance()
        {
            List<Vector3> adjustedPositons = new List<Vector3>();
            foreach (var shape in Shapes)
            {
                var posBlend = shape.Key.Position;
                List<int> originalVerticesIDS = Draw.VCPOLYIN.Select(c => c.Item2).ToList();
                Dictionary<int, Vector3> mappings = new Dictionary<int, Vector3>();
                foreach (var tup_id_vec in Draw.VCPOLYIN)
                {
                    if (!mappings.ContainsKey(tup_id_vec.Item2))
                    {
                        mappings.Add(tup_id_vec.Item2, tup_id_vec.Item3);
                    }
                }
                var positions = Draw.VCPOLYIN.Select(c => c.Item3).ToArray();
                List<int> updatedVertexMapping = Draw.VertexMapping.ToList();


                Vector3[] NewVectices = new Vector3[updatedVertexMapping.Count];

                for (var i = 0; i < updatedVertexMapping.Count; i++)
                {
                    var v = posBlend[originalVerticesIDS[updatedVertexMapping[i]]];
                    float x = -1 * v.x; float y = v.y; float z = -1 * v.z;
                    Vector3 vec3 = new Vector3(x, y, z);
                    NewVectices[i] = vec3;
                }
                adjustedPositons.AddRange(NewVectices);
            }
            return adjustedPositons;
        }


       


    }

}

// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Linq.Expressions;
using SharpFont;
using Silk.NET.Maths;
using Stride.Core;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;

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

        bool IsVertexDataUpdated2BlendShapeData;

        bool IsVertexDataUpdated2BlendShapeWeight;

        public Dictionary<Shape, float> _Shapes=new Dictionary<Shape, float>();
        public Dictionary<Shape, float> Shapes 
        { 
            get { return _Shapes; } 
            set { 
                _Shapes = value; 
                OnShapesUpdated(); 
            } 
        }

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
            return Shapes?.Keys?.Select(c => c.Name).ToArray();
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
                if (Shapes[shape] != weight)
                {
                    Shapes[shape] = weight;
                    OnWeightChanged();
                }
            }
        }
             
        public void UpdateBlendShapeImpact()
        {
            if(!IsVertexDataUpdated2BlendShapeData)
            { 
                ProcessBlendShapes();
            }
            else if (!IsVertexDataUpdated2BlendShapeWeight)
            {
                ProcessBlendShapes();
            }
            IsVertexDataUpdated2BlendShapeData = true;
            IsVertexDataUpdated2BlendShapeWeight = true;
        }

   
        /// <summary>
        /// CORE BLENDSHAPE PROCESSING, CAN BE EXPENSIVE THEREFORE DONE A BLEND SHAPE IS ADDED THROUGH A FILE IMPORT OR PROGRAMATICALLY, OR THE SHAPE DEFINITION UPDATES PROGRAMATICALLY
        /// 1. Update BDATAMAT to default
        /// 2.VISIT EACH BLENDHSHAPE UPDATE TO NET IMPACT on vertex =( WEIGHT * NEXT IMPACT)
        /// </summary>
        unsafe void ProcessBlendShapes()
        {
            fixed (byte* bp = Draw.VertexData)
            {
                int _counter = 0;
                for (int i = 0; i < Draw.VertexCount; i++)
                {
                    Vector3 netImpact = Vector3.Zero;
                    foreach (var shape in Shapes)
                    {
                        if (shape.Key.VertexImpactedByBlendShapes.Contains(i))
                        {
                            ++_counter;
                            var position = shape.Key.Position;
                            var impact = new Vector3(position[i].X - Draw.VerticesOriginal[i].X, position[i].Y - Draw.VerticesOriginal[i].Y, position[i].Z - Draw.VerticesOriginal[i].Z);
                            var impactWeighted = impact * shape.Value;
                            netImpact += impactWeighted;
                        }
                    }

                    Vector3* v = (Vector3*)(bp+ i*56);
                    v->X = Draw.VerticesOriginal[i].X + netImpact.X;
                    v->Y = Draw.VerticesOriginal[i].Y + netImpact.Y;
                    v->Z = Draw.VerticesOriginal[i].Z + netImpact.Z;
                }
            }
        }

        public void AddBlendShapes(Shape shape, float weight)
        {
            (Shapes ??= new()).Add(shape, weight);
            OnShapesUpdated();
        }

        public void OnShapesUpdated()
        {
            Shapes?.Keys?.ForEach(c => c.SetVextexImpacted(Draw));
            IsVertexDataUpdated2BlendShapeData = false;
        }

        public void OnWeightChanged()
        {
            IsVertexDataUpdated2BlendShapeWeight = false;
        }

    }

}

// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;

using Xenko.Core;
using Xenko.Core.Mathematics;

namespace Xenko.Rendering
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
            if (meshDraw == null) throw new ArgumentNullException("meshDraw");
            if (parameters == null) throw new ArgumentNullException("parameters");
            Draw = meshDraw;
            Parameters = parameters;
        }

        public MeshDraw Draw { get; set; }

        public int MaterialIndex { get; set; }
        
        public ParameterCollection Parameters { get; private set; }
        
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
    }
}

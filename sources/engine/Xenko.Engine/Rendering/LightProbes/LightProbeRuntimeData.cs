// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Xenko.Core;
using Xenko.Core.Collections;
using Xenko.Core.Diagnostics;
using Xenko.Core.Mathematics;
using Xenko.Core.Serialization.Contents;
using Xenko.Engine;
using Xenko.Games;
using Xenko.Graphics;
using Xenko.Rendering;
using Xenko.Rendering.ComputeEffect.LambertianPrefiltering;
using Xenko.Rendering.Images.SphericalHarmonics;
using Xenko.Rendering.LightProbes;
using Xenko.Rendering.Skyboxes;

namespace Xenko.Rendering.LightProbes
{
    public class LightProbeRuntimeData
    {
        /// <summary>
        /// Can be used to setup a link to a source.
        /// Typically, this might be a lightprobe component.
        /// </summary>
        public object[] LightProbes;

        // Computed data
        public Vector3[] Vertices;
        public int UserVertexCount;
        public FastList<BowyerWatsonTetrahedralization.Tetrahedron> Tetrahedra;
        public FastList<BowyerWatsonTetrahedralization.Face> Faces;

        // Data to upload to GPU
        public Color3[] Coefficients;
        public Vector4[] Matrices;
        public Int4[] LightProbeIndices;
    }
}

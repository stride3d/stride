﻿// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Sean Boettger <sean@whypenguins.com>
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;
using Stride.Core;
using Stride.Shaders;

namespace Stride.Rendering.Voxels
{
    [DataContract(DefaultMemberMode = DataMemberMode.Default)]
    [Display("Beam")]
    public class VoxelMarchBeam : IVoxelMarchMethod
    {
        public int Steps = 9;
        public float StepScale = 1.0f;
        public float BeamDiameter = 1.0f;
        public VoxelMarchBeam()
        {

        }
        public VoxelMarchBeam(int steps, float stepScale, float diameter)
        {
            Steps = steps;
            StepScale = stepScale;
            BeamDiameter = diameter;
        }
        public ShaderSource GetMarchingShader(int attrID)
        {
            var mixin = new ShaderMixinSource();
            mixin.Mixins.Add(new ShaderClassSource("VoxelMarchBeam", Steps, StepScale, BeamDiameter));
            mixin.Macros.Add(new ShaderMacro("AttributeID", attrID));
            return mixin;
        }

        public void UpdateMarchingLayout(string compositionName)
        {
        }
        public void ApplyMarchingParameters(ParameterCollection parameters)
        {
        }
    }
}

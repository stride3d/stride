// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;

using Stride.Core.Diagnostics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Shaders;

namespace Stride.Rendering.Materials
{
    public class MaterialGenerator
    {
        public static MaterialShaderResult Generate(MaterialDescriptor materialDescriptor, MaterialGeneratorContext context, string rootMaterialFriendlyName)
        {
            if (materialDescriptor == null) throw new ArgumentNullException("materialDescriptor");
            if (context == null) throw new ArgumentNullException("context");

            var result = new MaterialShaderResult();
            context.Log = result;

            result.Material = context.Material;

            context.PushMaterial(materialDescriptor, rootMaterialFriendlyName);
            
            context.Step = MaterialGeneratorStep.PassesEvaluation;
            materialDescriptor.Visit(context);

            context.Step = MaterialGeneratorStep.GenerateShader;
            for (int pass = 0; pass < context.PassCount; ++pass)
            {
                // TODO: It would be better if the passes would get executed like this:
                // [object0 pass0][object0 pass1] [object1 pass0][object1 pass1]
                // Instead of like this:
                // [object0 pass0][object1 pass0] [object0 pass1][object1 pass1]
                //
                // Caveat: While the first method improves the depth sorting (for transparents), it also causes more state changes.

                var materialPass = context.PushPass();

                context.PushLayer(null);
                materialDescriptor.Visit(context);
                context.PopLayer();

                materialPass.Parameters.Set(MaterialKeys.VertexStageSurfaceShaders, context.ComputeShaderSource(MaterialShaderStage.Vertex));
                materialPass.Parameters.Set(MaterialKeys.DomainStageSurfaceShaders, context.ComputeShaderSource(MaterialShaderStage.Domain));
                materialPass.Parameters.Set(MaterialKeys.PixelStageSurfaceShaders, context.ComputeShaderSource(MaterialShaderStage.Pixel));

                materialPass.Parameters.Set(MaterialKeys.VertexStageStreamInitializer, context.GenerateStreamInitializers(MaterialShaderStage.Vertex));
                materialPass.Parameters.Set(MaterialKeys.DomainStageStreamInitializer, context.GenerateStreamInitializers(MaterialShaderStage.Domain));
                materialPass.Parameters.Set(MaterialKeys.PixelStageStreamInitializer, context.GenerateStreamInitializers(MaterialShaderStage.Pixel));

                context.PopPass();
            }
            context.PopMaterial();

            return result;
        }
    }
}

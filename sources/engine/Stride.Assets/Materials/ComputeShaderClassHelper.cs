// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Shaders.Core;
using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Assets.Materials
{
    public static class ComputeShaderClassHelper
    {
        private static readonly Dictionary<string, Type> ComputeColorParameterTypeMapping = new Dictionary<string, Type>
        {
            {"Texture2D", typeof(ComputeColorParameterTexture) },
            {"bool", typeof(ComputeColorParameterBool) },
            {"int", typeof(ComputeColorParameterInt) },
            {"float", typeof(ComputeColorParameterFloat) },
            {"float2", typeof(ComputeColorParameterFloat2) },
            {"float3", typeof(ComputeColorParameterFloat3) },
            {"float4", typeof(ComputeColorParameterFloat4) },
            {"SamplerState", typeof(ComputeColorParameterSampler) },
        };

        public static Type GetComputeColorParameterType(string typeName)
        {
            Type type;
            ComputeColorParameterTypeMapping.TryGetValue(typeName, out type);
            return type;
        }

        public static ShaderClass ParseReferencedShader<T>(this ComputeShaderClassBase<T> node, IDictionary<string, string> projectShaders)
            where T : class, IComputeNode
        {
            string source;
            if (projectShaders.TryGetValue(node.MixinReference, out source))
            {
                try
                {
                    var parsed = SDSLParser.Parse(source);
                    if (parsed.Errors.Count > 0)
                        return null;
                    if (parsed.AST is not ShaderFile sf)
                        return null;

                    return sf.RootDeclarations.Concat(sf.Namespaces.SelectMany(x => x.Declarations)).OfType<ShaderClass>().SingleOrDefault();
                }
                catch
                {
                    // TODO: output messages
                    return null;
                }
            }

            return null;
        }
    }
}

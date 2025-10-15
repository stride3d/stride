using Stride.Shaders.Spirv.Core.Buffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders.Spirv.Core;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Building;
public partial class SpirvBuilder
{
    public static void BuildInheritanceList(IExternalShaderLoader shaderLoader, NewSpirvBuffer buffer, List<ShaderClassSource> inheritanceList)
    {
        // Build shader name mapping
        var shaderMapping = new Dictionary<int, ShaderClassSource>();
        foreach (var i in buffer)
            if (i.Op == Specification.Op.OpSDSLImportShader && (OpSDSLImportShader)i is { } importShader)
                shaderMapping[importShader.ResultId] = new ShaderClassSource(importShader.ShaderName);

        // Check inheritance
        foreach (var i in buffer)
        {
            if (i.Op == Specification.Op.OpSDSLMixinInherit && (OpSDSLMixinInherit)i is { } inherit)
            {
                var shaderName = shaderMapping[inherit.Shader];
                BuildInheritanceList(shaderLoader, shaderName, inheritanceList);
            }
        }
    }

    public static void BuildInheritanceList(IExternalShaderLoader shaderLoader, ShaderClassSource classSource, List<ShaderClassSource> inheritanceList)
    {
        if (!inheritanceList.Contains(classSource))
        {
            if (classSource.GenericArguments != null && classSource.GenericArguments.Length > 0)
                throw new NotImplementedException();

            var shader = GetOrLoadShader(shaderLoader, classSource.ClassName);
            BuildInheritanceList(shaderLoader, shader, inheritanceList);
            inheritanceList.Add(classSource);
        }
    }

    public static NewSpirvBuffer GetOrLoadShader(IExternalShaderLoader shaderLoader, string className)
    {
        if (!shaderLoader.LoadExternalBuffer(className, out var buffer))
            throw new InvalidOperationException($"Could not load shader [{className}]");

        return buffer;
    }
}

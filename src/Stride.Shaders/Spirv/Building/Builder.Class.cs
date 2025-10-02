using Stride.Shaders.Spirv.Core.Buffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Shaders.Spirv.Core;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Building;
public partial class SpirvBuilder
{
    public static void BuildInheritanceList(IExternalShaderLoader shaderLoader, NewSpirvBuffer buffer, List<string> inheritanceList)
    {
        // Build shader name mapping
        var shaderMapping = new Dictionary<int, string>();
        foreach (var i in buffer)
            if (i.Op == Specification.Op.OpSDSLImportShader && (OpSDSLImportShader)i is { } importShader)
                shaderMapping[importShader.ResultId] = importShader.ShaderName;

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

    public static void BuildInheritanceList(IExternalShaderLoader shaderLoader, string shaderName, List<string> inheritanceList)
    {
        if (!inheritanceList.Contains(shaderName))
        {
            var shader = GetOrLoadShader(shaderLoader, shaderName);
            BuildInheritanceList(shaderLoader, shader, inheritanceList);
            inheritanceList.Add(shaderName);
        }
    }

    public static NewSpirvBuffer GetOrLoadShader(IExternalShaderLoader shaderLoader, string name)
    {
        if (!shaderLoader.LoadExternalBuffer(name, out var buffer))
            throw new InvalidOperationException($"Could not load shader [{name}]");

        return buffer;
    }
}

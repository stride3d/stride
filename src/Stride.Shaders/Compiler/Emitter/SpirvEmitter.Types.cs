using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spv.Generator;
using Stride.Shaders.Parsing.AST.Shader;
using static Spv.Specification;

namespace Stride.Shaders.Spirv;

public partial class SpirvEmitter : Module
{
    static string[] nativeIntTypes = {
        "sbyte",
        "short",
        "int",
        "long"
    };
    static string[] nativeUintTypes = {
        "byte",
        "ushort",
        "uint",
        "ulong"
    };

    static string[] nativeFloatTypes = {
        "half",
        "float",
        "double",
    };

    public int Width(string v) => v switch {
        "sbyte" or "byte"=> 8,
        "short" or "half" => 16,
        "int" or "float" => 32,
        "long" or "double" => 64,
        _ => throw new NotImplementedException()
    };


    void CreateNativeTypes()
    {
        foreach(var t in nativeFloatTypes)
        {
            ShaderTypes[t] = TypeFloat(Width(t));
        }
        foreach(var t in nativeIntTypes)
        {
            ShaderTypes[t] = TypeInt(Width(t), 1);
        }
        foreach(var t in nativeUintTypes)
        {
            ShaderTypes[t] = TypeInt(Width(t), 0);
        }
        // vectors
        foreach(var t in nativeFloatTypes.Concat(nativeIntTypes).Concat(nativeUintTypes))
        {
            for (int i = 1; i < 5; i++)
            {
                ShaderTypes[t + i] = TypeVector(ShaderTypes[t],i);
            }
        }
        // Matrices
        foreach(var t in nativeFloatTypes.Concat(nativeIntTypes).Concat(nativeUintTypes))
        {
            for (int i = 1; i < 5; i++)
            {
                for (int j = 1; j < 5; j++)
                {
                    ShaderTypes[t + i + "x" + j] = TypeMatrix(ShaderTypes[t+i], j);
                }
            }
        }
    }
}

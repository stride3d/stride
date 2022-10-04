using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spv.Generator;
using Stride.Shaders.Parsing.AST.Shader;
using Stride.Shaders.Parsing.AST.Shader.Analysis;
using static Spv.Specification;

namespace Stride.Shaders.Spirv;

public partial class SpirvEmitter : Module
{

    Instruction GetSpvType(ISymbolType t)
    {
        return t switch
        {
            ScalarType s => AsSpvType(s.ToString()),
            VectorType s => AsSpvType(s.ToString()),
            MatrixType s => AsSpvType(s.ToString()),
            CompositeType c => ShaderTypes[c.Name].SpvType,
            _ => throw new NotImplementedException()
        };
    }
    int ConvertBuiltin(string? semantic)
    {
        return semantic switch
        {
            "SV_Position" => (int)BuiltIn.Position,
            "TEXCOORD" => (int)BuiltIn.FragCoord,
            "SV_DEPTH" => (int)BuiltIn.FragDepth,
            _ => -1
        };
    }
    void CreateStructs(ShaderProgram program)
    {
        foreach (var s in program.Symbols.GetAllStructTypes().Cast<CompositeType>())
        {
            var fields = new List<Instruction>(s.Fields.Count);
            foreach (var f in s.Fields)
            {
                var spv = GetSpvType(f.Value);
                if (s.HasSemantics && s.Semantics.TryGetValue(f.Key, out var semantic) && semantic != null)
                {
                    var possible = ConvertBuiltin(semantic);
                    if(possible > -1)
                        Decorate(spv, Decoration.BuiltIn, (LiteralInteger)possible);
                    else
                        Decorate(spv, Decoration.Location, (LiteralInteger)Semantics[semantic]);
                }
                fields.Add(spv);

            }
            ShaderTypes[s.Name] =
                new SpvStruct(
                    TypeStruct(
                        true,
                        fields.ToArray()
                    ),
                    s
                );
            Name(ShaderTypes[s.Name].SpvType, s.Name);
            for (int i = 0; i < s.Fields.Count; i++)
            {
                MemberName(ShaderTypes[s.Name].SpvType, i, s.Fields.Keys[i]);
            }
        }
    }
}

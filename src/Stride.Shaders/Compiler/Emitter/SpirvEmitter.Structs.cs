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
    void CreateStructs(ShaderProgram program)
    {
        foreach (var s in program.Symbols.GetAllStructTypes().Cast<CompositeType>())
        {
            var fields = s.Fields.Values.Select(x => GetSpvType(x)).ToArray();
            foreach(var f in fields)
                Decorate(f,Decoration.HlslSemanticGOOGLE,new LiteralString("POSITION"));
            ShaderTypes[s.Name] = 
                new SpvStruct(
                    TypeStruct(
                        true,
                        fields
                    ),
                    s
                );
            Name(ShaderTypes[s.Name].SpvType,s.Name);
            for (int i = 0; i < s.Fields.Count; i++)
            {
                MemberName(ShaderTypes[s.Name].SpvType,i,s.Fields.Keys[i]);
            }
        }
    }
}

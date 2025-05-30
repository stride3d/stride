using Stride.Shaders.Core;
using Stride.Shaders.Core.Analysis;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core.Buffers;
using System.Runtime.InteropServices;
using Stride.Shaders.Spirv.Core;

namespace Stride.Shaders.Parsing.SDSL.AST;




public class ShaderClass(Identifier name, TextLocation info) : ShaderDeclaration(info)
{
    public Identifier Name { get; set; } = name;
    public List<ShaderElement> Elements { get; set; } = [];
    public ShaderParameterDeclarations? Generics { get; set; }
    public List<Mixin> Mixins { get; set; } = [];

    public Dictionary<int, SymbolType> ProcessNameAndTypes(SpirvBuffer buffer, out Dictionary<int, string> names, out Dictionary<int, SymbolType> types)
    {
        var memberNames = new Dictionary<(int, int), string>();
        names = new Dictionary<int, string>();
        types = new Dictionary<int, SymbolType>();
        foreach (var instruction in buffer)
        {
            if (instruction.OpCode == SDSLOp.OpName)
            {
                var nameInstruction = instruction.UnsafeAs<RefOpName>();
                names.Add(nameInstruction.Target, nameInstruction.Name.Value);
            }
            else if (instruction.OpCode == SDSLOp.OpMemberName)
            {
                var nameInstruction = instruction.UnsafeAs<RefOpMemberName>();
                memberNames.Add((nameInstruction.Type, (int)nameInstruction.Member.Words), nameInstruction.Name.Value);
            }
            else if (instruction.OpCode == SDSLOp.OpTypeFloat)
            {
                var floatInstruction = instruction.UnsafeAs<RefOpTypeFloat>();
                if (floatInstruction.FloatingPointEncoding != 0)
                    throw new InvalidOperationException();

                types.Add(floatInstruction.ResultId, floatInstruction.Width.Words switch
                {
                    16 => ScalarType.From("half"),
                    32 => ScalarType.From("float"),
                    64 => ScalarType.From("double"),
                });
            }
            else if (instruction.OpCode == SDSLOp.OpTypeVoid)
            {
                types.Add(instruction.ResultId!.Value, ScalarType.From("void"));
            }
            else if (instruction.OpCode == SDSLOp.OpTypeVector)
            {
                var vectorInstruction = instruction.UnsafeAs<RefOpTypeVector>();
                var innerType = (ScalarType)types[vectorInstruction.ComponentType];
                types.Add(instruction.ResultId!.Value, new VectorType(innerType, (int)vectorInstruction.ComponentCount.Words));
            }
            else if (instruction.OpCode == SDSLOp.OpTypeStruct)
            {
                var structInstruction = instruction.UnsafeAs<RefOpTypeStruct>();
                var structName = names[instruction.ResultId!.Value];
                var fields = new List<(string Name, SymbolType Type)>();
                throw new NotImplementedException();
                types.Add(instruction.ResultId!.Value, new StructType(structName, fields));
            }
        }

        return types;
    }

    public override void ProcessSymbol(SymbolTable table)
    {
        foreach (var mixin in Mixins)
        {
            table.ShaderLoader.LoadExternalReference(mixin.Name, out var bytecode);
            var buffer = new SpirvBuffer(MemoryMarshal.Cast<byte, int>(bytecode));

            ProcessNameAndTypes(buffer, out var names, out var types);
            foreach (var instruction in buffer)
            {
                if (instruction.OpCode == SDSLOp.OpVariable)
                {
                    var variableInstruction = instruction.UnsafeAs<RefOpVariable>();
                    var variableName = names[variableInstruction.ResultId.Value];
                    var variableType = types[variableInstruction.ResultType];

                    var sid = new SymbolID(variableName, SymbolKind.Variable, Storage.Stream);
                    table.RootSymbols.Add(sid, new(sid, variableType));
                }
            }
        }

        foreach (var member in Elements)
        {
            if (member is ShaderMethod func)
            {
                func.ReturnTypeName.ProcessSymbol(table);
                var ftype = new FunctionType(func.ReturnTypeName.Type, []);
                foreach (var arg in func.Parameters)
                {
                    arg.TypeName.ProcessSymbol(table);
                    var argSym = arg.TypeName.Type;
                    table.DeclaredTypes.TryAdd(argSym.ToString(), argSym);
                    arg.Type = argSym;
                    ftype.ParameterTypes.Add(arg.Type);
                }
                func.Type = ftype;

                table.RootSymbols.Add(new(func.Name, SymbolKind.Method), new(new(func.Name, SymbolKind.Method), func.Type));
                table.DeclaredTypes.TryAdd(func.Type.ToString(), func.Type);
            }
            else if (member is ShaderMember svar)
            {
                svar.TypeName.ProcessSymbol(table);
                svar.Type = svar.TypeName.Type;
                var sid = 
                    new SymbolID
                    (
                        svar.Name,
                        svar.TypeModifier == TypeModifier.Const ? SymbolKind.Constant : SymbolKind.Variable,
                        svar.StreamKind switch
                        {
                            StreamKind.Stream or StreamKind.PatchStream => Storage.Stream,
                            _ => Storage.None
                        }
                    );
                var symbol = new Symbol(sid, svar.Type);
                //if (sid.Storage == Storage.Stream)
                //{
                //    table.Streams.Add(sid, symbol);
                //}
                //else
                {
                    table.RootSymbols.Add(sid, symbol);
                }
                table.DeclaredTypes.TryAdd(svar.Type.ToString(), svar.Type);
            }
        }

        /*var streams =
            new SymbolID
            (
                "streams",
                SymbolKind.Variable,
                Storage.None
            );
        table.RootSymbols.Add(streams, new(streams, new StreamsSymbol()));*/

        foreach (var member in Elements)
        {
            if (member is not ShaderMember)
                member.ProcessSymbol(table);
        }
    }


    public void Compile(CompilerUnit compiler, SymbolTable table)
    {
        compiler.Context.PutMixinName(Name);
        foreach(var member in Elements.OfType<ShaderMember>())
            member.Compile(table, this, compiler);
        foreach(var method in Elements.OfType<ShaderMethod>())
            method.Compile(table, this, compiler);
    }


    public override string ToString()
    {
        return
$"""
Class : {Name}
Generics : {string.Join(", ", Generics)}
Inherits from : {string.Join(", ", Mixins)}
Body :
{string.Join("\n", Elements)}
""";
    }
}


public class ShaderGenerics(Identifier typename, Identifier name, TextLocation info) : Node(info)
{
    public Identifier Name { get; set; } = name;
    public Identifier TypeName { get; set; } = typename;
}

public class Mixin(Identifier name, TextLocation info) : Node(info)
{
    public List<Identifier> Path { get; set; } = [];
    public Identifier Name { get; set; } = name;
    public ShaderExpressionList? Generics { get; set; }
    public override string ToString()
        => Generics switch
        {
            null => Name.Name,
            _ => $"{Name}<{Generics}>"
        };
}

public abstract class ShaderMixinValue(TextLocation info) : Node(info);
public class ShaderMixinExpression(Expression expression, TextLocation info) : ShaderMixinValue(info)
{
    public Expression Value { get; set; } = expression;
}
public class ShaderMixinIdentifier(Identifier identifier, TextLocation info) : ShaderMixinValue(info)
{
    public Identifier Value { get; set; } = identifier;
}
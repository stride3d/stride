using Stride.Shaders.Core;
using Stride.Shaders.Core.Analysis;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Spirv.Building;

namespace Stride.Shaders.Parsing.SDSL.AST;




public class ShaderClass(Identifier name, TextLocation info) : ShaderDeclaration(info)
{
    public Identifier Name { get; set; } = name;
    public List<ShaderElement> Elements { get; set; } = [];
    public ShaderParameterDeclarations? Generics { get; set; }
    public List<Mixin> Mixins { get; set; } = [];


    public override void ProcessSymbol(SymbolTable table)
    {
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
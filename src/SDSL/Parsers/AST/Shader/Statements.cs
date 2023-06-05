using Eto.Parse;
using SDSL.Parsing.AST.Shader;
using SDSL.Parsing.AST.Shader.Analysis;
using SDSL.ThreeAddress;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDSL.Parsing.AST.Shader;


public abstract class Statement : ShaderTokenTyped
{

    public IEnumerable<Register> LowCode { get; set; }
    public override ISymbolType InferredType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public override void TypeCheck(SymbolTable symbols, ISymbolType expected)
    {
        throw new NotImplementedException();
    }
}

public class EmptyStatement : Statement
{
    public override ISymbolType InferredType => ScalarType.VoidType;
    public override void TypeCheck(SymbolTable symbols, ISymbolType expected) { }
}

public abstract class Declaration : Statement
{
    public override ISymbolType InferredType => ScalarType.VoidType;
    public ISymbolType? TypeName { get; set; }
    public string VariableName { get; set; }

}

public class DeclareAssign : Declaration, IStaticCheck, IStreamCheck
{
    public AssignOpToken AssignOp { get; set; }
    public ShaderTokenTyped Value { get; set; }


    public DeclareAssign() { }
    public DeclareAssign(Match m, SymbolTable s)
    {
        Match = m;
        AssignOp = m["AssignOp"].StringValue.ToAssignOp();
        TypeName = s.PushType(m["ValueTypes"].StringValue, m["ValueTypes"]);
        VariableName = m["Variable"].StringValue;
        Value = (ShaderTokenTyped)GetToken(m["Value"], s);
    }

    public bool CheckStatic(SymbolTable s)
    {
        return Value is IStaticCheck sc &&
            sc.CheckStatic(s);
    }

    public bool CheckStream(SymbolTable s)
    {
        return Value is IStreamCheck sc &&
            sc.CheckStream(s);
    }
    public IEnumerable<string> GetUsedStream()
    {
        if (Value is IStreamCheck val)
            return val.GetUsedStream();
        return Enumerable.Empty<string>();
    }
    public IEnumerable<string> GetAssignedStream()
    {
        return Enumerable.Empty<string>();
    }
    public override void TypeCheck(SymbolTable symbols, ISymbolType expected)
    {
        Value.TypeCheck(symbols, TypeName);
    }
}

public class SimpleDeclare : Declaration
{
    public SimpleDeclare() { }
    public SimpleDeclare(Match m, SymbolTable s)
    {
        Match = m;
        VariableName = m["Variable"].StringValue;
        TypeName = s.PushType(m["ValueTypes"].StringValue, m["ValueTypes"]);

    }
    public override void TypeCheck(SymbolTable symbols, ISymbolType expected) { }
}

public class AssignChain : Statement, IStreamCheck, IStaticCheck, IVariableCheck
{
    public override ISymbolType InferredType => ScalarType.VoidType;

    public AssignOpToken AssignOp { get; set; }
    public bool StreamValue => AccessNames.Any() && AccessNames.First() == "streams";
    public List<string> AccessNames { get; set; }
    public ShaderTokenTyped Value { get; set; }
    public AssignChain(Match m, SymbolTable s)
    {
        Match = m;
        AssignOp = m["AssignOp"].StringValue.ToAssignOp();
        AccessNames = m.Matches.Where(x => x.Name == "Identifier").Select(x => x.StringValue).ToList();
        Value = (ShaderTokenTyped)GetToken(m["PrimaryExpression"], s);
    }

    public bool CheckStream(SymbolTable s)
    {
        return StreamValue || Value is IStreamCheck isc && isc.CheckStream(s);
    }

    public IEnumerable<string> GetAssignedStream()
    {
        if (StreamValue)
            return new List<string>() { AccessNames.ElementAt(1) };
        else
            return Enumerable.Empty<string>();
    }
    public IEnumerable<string> GetUsedStream()
    {
        if (Value is IStreamCheck v)
            return v.GetUsedStream();
        else
            return Enumerable.Empty<string>();
    }

    public bool CheckStatic(SymbolTable s)
    {
        return Value is IStaticCheck isc && isc.CheckStatic(s);
    }

    public void CheckVariables(SymbolTable s)
    {
        if (!s.Any(x => x.ContainsKey(this.AccessNames.First())))
            throw new Exception("Variable not exist");
        if (Value is IVariableCheck v) v.CheckVariables(s);
    }
    public override void TypeCheck(SymbolTable symbols, ISymbolType expected)
    {
        ISymbolType chainType = ScalarType.VoidType;
        foreach (var a in AccessNames)
        {
            var tmp = chainType;
            if (a == AccessNames.First())
            {
                if (!symbols.TryGetVarType(a, out chainType))
                {
                    symbols.AddError(Match, $"Field `{a}` doesn't exist in type `{tmp}`");
                    return;
                }
            }
            else if (!chainType.TryAccessType(a, out chainType))
            {
                symbols.AddError(Match, $"Field `{a}` doesn't exist in type `{tmp}`");
                return;
            }
        }
        Value.TypeCheck(symbols, null); // Variable check ?
        if (!chainType.Equals(Value.InferredType))
            symbols.AddError(Match, $"Cannot cast `{chainType}` to `{Value.InferredType}`");
    }
}

public class ReturnStatement : Statement, IStreamCheck, IStaticCheck
{
    public override ISymbolType InferredType => ReturnValue?.InferredType ?? ScalarType.VoidType;

    public ShaderTokenTyped? ReturnValue { get; set; }
    public ReturnStatement(Match m, SymbolTable s)
    {
        Match = m;
        throw new NotImplementedException();
        // if (m.HasMatches)
        //     ReturnValue = (ShaderTokenTyped)GetToken(m["PrimaryExpression"]);
    }

    public bool CheckStream(SymbolTable s)
    {
        return ReturnValue is IStreamCheck sc && sc.CheckStream(s);
    }

    public IEnumerable<string> GetUsedStream()
    {
        if (ReturnValue is IStreamCheck isc)
            return isc.GetUsedStream();
        return Enumerable.Empty<string>();
    }
    public IEnumerable<string> GetAssignedStream()
    {
        return Enumerable.Empty<string>();
    }

    public bool CheckStatic(SymbolTable s)
    {
        return ReturnValue is IStaticCheck sc && sc.CheckStatic(s);
    }
}

public class BlockStatement : Statement, IStreamCheck, IStaticCheck
{
    public IEnumerable<Statement> Statements { get; set; }
    public BlockStatement(Match m, SymbolTable s)
    {
        Match = m;
        throw new NotImplementedException();
        // Statements = m.Matches.Select(GetToken).Cast<Statement>().ToList();
    }

    public bool CheckStream(SymbolTable s)
    {
        return Statements.Any(x => x is IStreamCheck isc && isc.CheckStream(s));
    }
    public IEnumerable<string> GetUsedStream()
    {
        return Statements.OfType<IStreamCheck>().SelectMany(x => x.GetUsedStream());
    }
    public IEnumerable<string> GetAssignedStream()
    {
        return Statements.OfType<IStreamCheck>().SelectMany(x => x.GetAssignedStream());
    }

    public bool CheckStatic(SymbolTable s)
    {
        return Statements.Any(x => x is IStaticCheck isc && isc.CheckStatic(s));
    }
}

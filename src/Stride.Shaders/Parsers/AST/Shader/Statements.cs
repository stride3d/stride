using Eto.Parse;
using Spv.Generator;
using Stride.Shaders.Parsing.AST.Shader;
using Stride.Shaders.Parsing.AST.Shader.Analysis;
using Stride.Shaders.Spirv;
using Stride.Shaders.ThreeAddress;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shaders.Parsing.AST.Shader;


public abstract class Statement : ShaderTokenTyped
{

    public IEnumerable<Register> LowCode { get; set; }
    public override string InferredType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public string GetInferredType()
    {
        return InferredType;
    }

    public override void TypeCheck(SymbolTable symbols, string expected = "")
    {
        throw new NotImplementedException();
    }
}

public class EmptyStatement : Statement
{
    public override string InferredType => "void";
    public override void TypeCheck(SymbolTable symbols, string expected = "") { }
}

public abstract class Declaration : Statement
{
    public override string InferredType => "void";
    public string? TypeName { get; set; }
    public string VariableName { get; set; }
    public ShaderTokenTyped Value { get; set; }

}

public class DeclareAssign : Declaration, IStaticCheck, IStreamCheck
{
    public AssignOpToken AssignOp { get; set; }

    public DeclareAssign() { }
    public DeclareAssign(Match m)
    {
        Match = m;
        AssignOp = m["AssignOp"].StringValue.ToAssignOp();
        TypeName = m["Type"].StringValue;
        VariableName = m["Variable"].StringValue;
        Value = (ShaderTokenTyped)GetToken(m["Value"]);
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
}

public class AssignChain : Statement, IStreamCheck, IStaticCheck, IVariableCheck
{
    public override string InferredType => "void";

    public AssignOpToken AssignOp { get; set; }
    public bool StreamValue => AccessNames.Any() && AccessNames.First() == "streams";
    public IEnumerable<string> AccessNames { get; set; }
    public ShaderTokenTyped Value { get; set; }
    public AssignChain(Match m)
    {
        Match = m;
        AssignOp = m["AssignOp"].StringValue.ToAssignOp();
        AccessNames = m.Matches.Where(x => x.Name == "Identifier").Select(x => x.StringValue);
        Value = (ShaderTokenTyped)GetToken(m["PrimaryExpression"]);
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
        if(!s.Any(x => x.ContainsKey(this.AccessNames.First())))
            throw new Exception("Variable not exist");
        if(Value is IVariableCheck v) v.CheckVariables(s);
    }
}

public class ReturnStatement : Statement, IStreamCheck, IStaticCheck
{
    public override string InferredType => ReturnValue?.InferredType ?? "void";

    public ShaderTokenTyped? ReturnValue { get; set; }
    public ReturnStatement(Match m)
    {
        Match = m;
        if (m.HasMatches)
            ReturnValue = (ShaderTokenTyped)GetToken(m["PrimaryExpression"]);
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
    public BlockStatement(Match m)
    {
        Match = m;
        Statements = m.Matches.Select(GetToken).Cast<Statement>().ToList();
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

using Eto.Parse;
using Spv.Generator;
using Stride.Shaders.Parsing.AST.Shader;
using Stride.Shaders.Spirv;
using Stride.Shaders.ThreeAddress;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shaders.Parsing.AST.Shader;


public abstract class Statement : ShaderToken, ITyped
{
    public IEnumerable<Register> LowCode {get;set;}
    public virtual string InferredType{get => throw new NotImplementedException();set => throw new NotImplementedException();}

    public string GetInferredType()
    {
        return InferredType;
    }
}

public class EmptyStatement : Statement
{
    public override string InferredType => "void";
}

public class DeclareAssign : Statement
{
    public override string InferredType => "void";
    public AssignOpToken AssignOp { get; set; }
    public string TypeName { get; set; }
    public string VariableName { get; set; }
    public ShaderToken Value { get; set; }
    public DeclareAssign(){}
    public DeclareAssign(Match m )
    {
        Match = m;
        AssignOp = m["AssignOp"].StringValue.ToAssignOp();
        TypeName = m["Type"].StringValue;
        VariableName = m["Variable"].StringValue;
        Value = GetToken(m["Value"]);
    }
}

public class AssignChain : Statement
{
    public override string InferredType => "void";

    public AssignOpToken AssignOp { get; set; }
    public bool StreamValue => AccessNames.Any() && AccessNames.First() == "streams";
    public IEnumerable<string> AccessNames { get; set; }
    public ShaderToken Value { get; set; }
    public AssignChain(Match m)
    {
        Match = m;
        AssignOp = m["AssignOp"].StringValue.ToAssignOp();
        AccessNames = m.Matches.Where(x => x.Name == "Identifier").Select(x => x.StringValue);
        Value = GetToken(m["PrimaryExpression"]);
    }
}

public class ReturnStatement : Statement
{
    public override string InferredType => ReturnValue?.GetInferredType() ?? "void";
    
    public ITyped? ReturnValue {get;set;}
    public ReturnStatement(Match m)
    {
        Match = m;
        if(m.HasMatches)
            ReturnValue = (ITyped)GetToken(m["PrimaryExpression"]);
    }
}

public class BlockStatement : Statement 
{
    public IEnumerable<Statement> Statements {get;set;}
    public BlockStatement(Match m)
    {
        Match = m;
        Statements = m.Matches.Select(GetToken).Cast<Statement>().ToList();
    }
}

using Eto.Parse;
using Spv.Generator;
using Stride.Shaders.Parsing.AST.Shader;
using Stride.Shaders.Spirv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shaders.Parsing.AST.Shader;


public class Statement : ShaderToken 
{
    public List<Register> LowCode {get;set;} = new();
}

public class EmptyStatement : Statement {}

public class DeclareAssign : Statement
{
    public AssignOpToken AssignOp { get; set; }
    public string TypeName { get; set; }
    public string VariableName { get; set; }
    public ShaderToken Value { get; set; }
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
    public AssignOpToken AssignOp { get; set; }
    public bool StreamValue { get; set; }
    public IEnumerable<string> AccessNames { get; set; }
    public ShaderToken Value { get; set; }
    public AssignChain(Match m)
    {
        Match = m;
        AssignOp = m["AssignOp"].StringValue.ToAssignOp();
        StreamValue = m.Matches.First().StringValue == "stream";
        AccessNames = m.Matches.Where(x => x.Name == "Identifier").Select(x => x.StringValue);
        Value = GetToken(m["PrimaryExpression"]);
    }

}

public class ReturnStatement : Statement
{
    public ShaderToken? ReturnValue {get;set;}
    public ReturnStatement(Match m)
    {
        Match = m;
        if(m.HasMatches)
            ReturnValue = GetToken(m["PrimaryExpression"]);
    }
}

public class BlockStatement : Statement 
{
    public IEnumerable<ShaderToken> Statements {get;set;}
    public BlockStatement(Match m)
    {
        Match = m;
        Statements = m.Matches.Select(GetToken).ToList();
    }
}

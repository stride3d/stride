using Xunit;
using System.Linq;
using Stride.Shaders.Parsing;
using System.Collections.Generic;
using Stride.Shaders.Parsing.AST.Shader.Analysis;
using Stride.Shaders.Parsing.AST.Shader;
using Stride.Shaders.ThreeAddress;

namespace Stride.Shaders.Parsing.Test;

public class ASTTypeChecking
{
    [Fact]
    public void TypeCheckIntWithFloatCastedToInt()
    {
        var o =
            new Operation
            {
                Left = new NumberLiteral { Value = 5L },
                Right = new NumberLiteral { Value = 6L },
                Op = OperatorToken.Plus
            };

        var symbols = new SymbolTable();
        var s = new DeclareAssign() { TypeName = "int", VariableName = "dodo", AssignOp = AssignOpToken.Equal, Value = o };
        symbols.PushVar(s);
        var o2 =
            new Operation
            {
                Left = new VariableNameLiteral("dodo"),
                Right = new NumberLiteral { Value = 6L, InferredType = "float" },
                Op = OperatorToken.Plus
            };
        var s2 = new DeclareAssign() { VariableName = "dodo2", AssignOp = AssignOpToken.Equal, Value = o2 };
        symbols.PushVar(s2);
        Assert.Equal("int", s2.InferredType);
                
    }
    [Fact]
    public void TypeCheckInt()
    {
        var o =
            new Operation
            {
                Left = new NumberLiteral { Value = 5L },
                Right = new NumberLiteral { Value = 6L },
                Op = OperatorToken.Plus
            };

        var symbols = new SymbolTable();
        var s = new DeclareAssign() { TypeName = "int", VariableName = "dodo", AssignOp = AssignOpToken.Equal, Value = o };
        symbols.PushVar(s);
        var o2 =
            new Operation
            {
                Left = new VariableNameLiteral("dodo"),
                Right = new NumberLiteral { Value = 6L, InferredType = "int" },
                Op = OperatorToken.Plus
            };
        var s2 = new DeclareAssign() { VariableName = "dodo2", AssignOp = AssignOpToken.Equal, Value = o2 };
        symbols.PushVar(s2);
        Assert.Equal("int", s2.InferredType);
                
    }
    [Fact]
    public void TypeCheckFloat()
    {
        var o =
            new Operation
            {
                Left = new NumberLiteral { Value = 5L },
                Right = new NumberLiteral { Value = 6L },
                Op = OperatorToken.Plus
            };

        var symbols = new SymbolTable();
        var s = new DeclareAssign() { TypeName = "float", VariableName = "dodo", AssignOp = AssignOpToken.Equal, Value = o };
        symbols.PushVar(s);
        var o2 =
            new Operation
            {
                Left = new VariableNameLiteral("dodo"),
                Right = new NumberLiteral { Value = 6L, InferredType = "int" },
                Op = OperatorToken.Plus
            };
        var s2 = new DeclareAssign() { VariableName = "dodo2", AssignOp = AssignOpToken.Equal, Value = o2 };
        symbols.PushVar(s2);
        Assert.Equal("float", s2.InferredType);
                
    }
}
using Xunit;
using System.Linq;
using SDSL.Parsing;
using System.Collections.Generic;
using SDSL.Parsing.AST.Shader.Analysis;
using SDSL.Parsing.AST.Shader;
using SDSL.ThreeAddress;

namespace SDSL.Parsing.Test;

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
        var s = new DeclareAssign() { TypeName = new ScalarType("int"), VariableName = "dodo", AssignOp = AssignOpToken.Equal, Value = o };
        symbols.PushVar(s);
        var o2 =
            new Operation
            {
                Left = new VariableNameLiteral("dodo"),
                Right = new NumberLiteral { Value = 6L, InferredType = new ScalarType("float") },
                Op = OperatorToken.Plus
            };
        var s2 = new DeclareAssign() { VariableName = "dodo2", AssignOp = AssignOpToken.Equal, Value = o2 };
        symbols.PushVar(s2);
        Assert.Equal(new ScalarType("int"), s2.InferredType);
                
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
        var s = new DeclareAssign() { TypeName = new ScalarType("int"), VariableName = "dodo", AssignOp = AssignOpToken.Equal, Value = o };
        symbols.PushVar(s);
        var o2 =
            new Operation
            {
                Left = new VariableNameLiteral("dodo"),
                Right = new NumberLiteral { Value = 6L, InferredType = new ScalarType("int") },
                Op = OperatorToken.Plus
            };
        var s2 = new DeclareAssign() { VariableName = "dodo2", AssignOp = AssignOpToken.Equal, Value = o2 };
        symbols.PushVar(s2);
        Assert.Equal(new ScalarType("int"), s2.InferredType);
                
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
        var s = new DeclareAssign() { TypeName = new ScalarType("float"), VariableName = "dodo", AssignOp = AssignOpToken.Equal, Value = o };
        symbols.PushVar(s);
        var o2 =
            new Operation
            {
                Left = new VariableNameLiteral("dodo"),
                Right = new NumberLiteral { Value = 6L, InferredType = new ScalarType("int") },
                Op = OperatorToken.Plus
            };
        var s2 = new DeclareAssign() { VariableName = "dodo2", AssignOp = AssignOpToken.Equal, Value = o2 };
        symbols.PushVar(s2);
        Assert.Equal(new ScalarType("float"), s2.InferredType);
    }
}
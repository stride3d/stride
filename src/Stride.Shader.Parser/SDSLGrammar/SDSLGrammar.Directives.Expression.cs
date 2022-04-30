using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace Stride.Shader.Parser;
public partial class SDSLGrammar : Grammar
{
    public AlternativeParser DirectiveTerm = new();
    public AlternativeParser DirectiveMul = new();
    public AlternativeParser DirectiveSum = new();
    public AlternativeParser DirectiveTest = new();

    public AlternativeParser DirectiveExpr = new();
    public AlternativeParser DirectiveIncrementExpr = new();
    public AlternativeParser ParenDirectiveExpr = new();

    public SDSLGrammar UsingDirectiveExpression()
    {
        Inner = Literals;
        return this;
    }

    public void CreateDirectiveExpressions()
    {
        var parenMul = 
            LeftParen.Then(DirectiveMul).Then(RightParen);
        var parenSum = 
            LeftParen.Then(DirectiveSum).Then(RightParen);
        var parenTest = 
            LeftParen.Then(DirectiveTest).Then(RightParen);
        
        DirectiveIncrementExpr.Add(
            Identifier.Then(PlusPlus).Named("IdPostIncrement")
            // | Identifier.Then(MinusMinus).Named("IdPostDecrement")
            // | LeftParen.Then(Identifier).Then(RightParen).NotFollowedBy(MinusMinus).Then(PlusPlus).Named("ParenPostIncrement")
            // | LeftParen.Then(Identifier).Then(RightParen).Then(MinusMinus).Named("ParenPostDecrement")
        );
        var userDefinedTypes = Identifier.Except(Keywords);
        var increment = userDefinedTypes & "++";

        DirectiveTerm.Add(
            Literals
            | userDefinedTypes.Then("++")
            // | parenMul 
            // | parenSum 
            // | parenTest 
        );
            // | ParenDirectiveExpr;
        
        var mulOp = Star | Div | Mod;

        var multiply = DirectiveTerm.Then(Star).Then(DirectiveMul).Named("DirectiveMultiply");
        var divide = DirectiveTerm.Then(Div).Then(DirectiveMul).Named("DirectiveDivide");
        var moderand = DirectiveTerm.Then(Mod).Then(DirectiveMul).Named("DirectiveModerand");

        

        
        DirectiveMul.Add(
            DirectiveTerm - (multiply | divide | moderand)
            | multiply - (divide | moderand)
            | divide - moderand
            | moderand
        );
        
                
        var add = DirectiveMul.Then(Plus).Then(DirectiveSum).Named("DirectiveAdd");
        var subtract = DirectiveMul.Then(Minus).Then(DirectiveSum).Named("DirectiveSubtract");
        

        DirectiveSum.Add(
            DirectiveMul - (add | subtract)
            | add - subtract
            | subtract
        );
        
        var greater = DirectiveSum.Then(Greater).Then(DirectiveTest).Named("DirectiveGreater");
        var less = DirectiveSum.Then(Less).Then(DirectiveTest).Named("DirectiveLess");
        var greaterEqual = DirectiveSum.Then(GreaterEqual).Then(DirectiveTest).Named("DirectiveGreaterEqual");
        var lessEqual = DirectiveSum.Then(LessEqual).Then(DirectiveTest).Named("DirectiveLessEqual");
        
        DirectiveTest.Add(
            DirectiveSum - (greater | less | greaterEqual | lessEqual)
            | greater - (less | greaterEqual | lessEqual)
            | less - (greaterEqual | lessEqual)
            | greaterEqual - lessEqual
            | lessEqual
        );

        
        ParenDirectiveExpr.Add(
            LeftParen.Then(DirectiveExpr).Then(RightParen)
        );

        

        var methodCall = Identifier.Then(LeftParen).Then(RightParen).Named("DirectiveMethodCall");

        DirectiveExpr.Add(
            DirectiveTest
            | ParenDirectiveExpr
            // | DirectiveIncrementExpr - methodCall
            // | methodCall
        );

        


        // IncrementDirectiveExpr ::=
        // literal postfixUnaryOperator
        // | Identifier postfixUnaryOperator
        // | ParenDirectiveExpression postfixUnaryOperator 
        // | prefixUnaryOperator Identifier - postfixUnaryOperator
        // | prefixUnaryOperator ParenDirectiveExpression - postfixUnaryOperator

        // directiveExpression ::=
        // TestDirective
        // | IncrementExpr - (MethodCallDirective)
        // | MethodCallDirective
    }
}
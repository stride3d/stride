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
        Inner = DirectiveExpr;
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
        
        
        var userDefinedTypes = Identifier.Except(Keywords);
        
        var incrementOp = PlusPlus | MinusMinus;
        
        var postfixIncrement = 
            (Identifier & "++").Named("Increment")
            | (Identifier & "--").Named("Decrement");
        var prefixIncrement = 
            ("++" & Identifier).Named("Increment")
            | ("--" & Identifier).Named("Decrement");
        
        DirectiveIncrementExpr.Add(
            prefixIncrement
            | postfixIncrement
        );

        DirectiveTerm.Add(
            Literals
            | DirectiveIncrementExpr
            // | DirectiveIncrementExpr
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
            DirectiveMul.FollowedBy(incrementOp.Optional()) - ( add | subtract)
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
            | ParenDirectiveExpr.Named("ParenthesisExpr")
            
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
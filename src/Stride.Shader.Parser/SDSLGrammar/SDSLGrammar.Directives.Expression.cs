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
        var wsOrTabs = WhiteSpace.Or("\t").Repeat(0);
        wsOrTabs.SkipUntil = true;
        var userDefinedTypes = Identifier.Except(Keywords);
        
        var incrementOp = 
            PlusPlus 
            | MinusMinus;
        
        // Identifier.Then(WhiteSpace.Repeat(0).Until("++",false,true)).Named("Something");

        var postfixIncrement = 
            (Identifier & incrementOp.Named("IncrementOp")).Named("PostIncrement");
       
        var prefixIncrement = 
            (incrementOp.Named("IncrementOp") & Identifier).Named("PostIncrement");

        DirectiveIncrementExpr.Add(
            prefixIncrement
            | postfixIncrement
        );
        DirectiveIncrementExpr.SeparateChildrenBy(WhiteSpace.Repeat(0));



        DirectiveTerm.Add(
            Literals
            | DirectiveIncrementExpr
            | ParenDirectiveExpr.Named("ParenthesisExpr")
        );
        // DirectiveTerm.SeparateChildrenBy(WhiteSpace.Repeat(0));
        
        
        var mulOp = Star | Div | Mod;

        var multiply = DirectiveTerm.Then(Star).Then(DirectiveMul).Named("DirectiveMultiply");
        var divide = DirectiveTerm.Then(Div).Then(DirectiveMul).Named("DirectiveDivide");
        var moderand = DirectiveTerm.Then(Mod).Then(DirectiveMul).Named("DirectiveModerand");

        
        DirectiveMul.Add(
            ParenDirectiveExpr
            | DirectiveTerm - (multiply | divide | moderand)
            | multiply - (divide | moderand)
            | divide - moderand
            | moderand
        );
        DirectiveMul.SeparateChildrenBy(WhiteSpace.Repeat(0));

        
                
        var add = DirectiveMul.Then(Plus).Then(DirectiveSum).Named("DirectiveAdd");
        var subtract = DirectiveMul.Then(Minus).Then(DirectiveSum).Named("DirectiveSubtract");
        
        

        DirectiveSum.Add(
            ParenDirectiveExpr
            | postfixIncrement
            | DirectiveMul.NotFollowedBy(Plus | Minus) //- (add | subtract)
            | add - subtract
            // | subtract
        );
        DirectiveSum.SeparateChildrenBy(WhiteSpace.Repeat(0));

        
        var greater = DirectiveSum.Then(Greater).Then(DirectiveTest).Named("DirectiveGreater");
        var less = DirectiveSum.Then(Less).Then(DirectiveTest).Named("DirectiveLess");
        var greaterEqual = DirectiveSum.Then(GreaterEqual).Then(DirectiveTest).Named("DirectiveGreaterEqual");
        var lessEqual = DirectiveSum.Then(LessEqual).Then(DirectiveTest).Named("DirectiveLessEqual");
        
        DirectiveTest.Add(
            ParenDirectiveExpr
            | DirectiveSum.NotFollowedBy(Greater | Less | GreaterEqual | LessEqual)
            | greater.NotFollowedBy(Less | GreaterEqual | LessEqual)
            | less.NotFollowedBy(GreaterEqual | LessEqual)
            | greaterEqual.NotFollowedBy(LessEqual)
            | lessEqual
        );
        DirectiveTest.SeparateChildrenBy(WhiteSpace.Repeat(0));


        
        ParenDirectiveExpr.Add(
            LeftParen.Then(DirectiveExpr).Then(RightParen)
        );
        ParenDirectiveExpr.SeparateChildrenBy(WhiteSpace.Repeat(0));

        

        var methodCall = Identifier.Then(LeftParen).Then(RightParen).Named("DirectiveMethodCall");

        DirectiveExpr.Add(
            ParenDirectiveExpr
            | DirectiveTest - methodCall      
            | methodCall
        );
        DirectiveExpr.SeparateChildrenBy(WhiteSpace.Repeat(0));
    }
}
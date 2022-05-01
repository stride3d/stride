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
        var ls = SingleLineWhiteSpace.Repeat(0);
        var ls1 = SingleLineWhiteSpace.Repeat(1);

        var incrementOp = 
            PlusPlus 
            | MinusMinus;
        
        // Identifier.Then(SingleLineWhiteSpace.Until("++",false,true)).Named("Something");

        var postfixIncrement = 
            Identifier.Then(incrementOp.Named("IncrementOp"));
       
        var prefixIncrement = 
            incrementOp.Named("IncrementOp").Then(Identifier);

        DirectiveIncrementExpr.Add(
            prefixIncrement.SeparatedBy(ls)
            | postfixIncrement.SeparatedBy(ls)
        );
        // DirectiveIncrementExpr.SeparateChildrenBy(SingleLineWhiteSpace);



        DirectiveTerm.Add(
            Literals
            | DirectiveIncrementExpr
            | ParenDirectiveExpr.Named("ParenthesisExpr")
        );

        var multiply = DirectiveTerm.Then(Star).Then(DirectiveMul).SeparatedBy(ls);
        var divide = DirectiveTerm.Then(Div).Then(DirectiveMul).SeparatedBy(ls);
        var moderand = DirectiveTerm.Then(Mod).Then(DirectiveMul).SeparatedBy(ls);

        
        DirectiveMul.Add(
            ParenDirectiveExpr
            | DirectiveTerm - (multiply | divide | moderand)
            | (multiply - (divide | moderand)).Named("DirectiveMult")
            | (divide - moderand).Named("DirectiveDiv")
            | moderand.Named("DirectiveMod")
        );
        // DirectiveMul.SeparateChildrenBy(SingleLineWhiteSpace);

        
                
        var add = DirectiveMul.Then(Plus).Then(DirectiveSum).SeparatedBy(ls);
        var subtract = DirectiveMul.Then(Minus).Then(DirectiveSum).SeparatedBy(ls);
        
        

        DirectiveSum.Add(
            ParenDirectiveExpr
            | postfixIncrement.SeparatedBy(ls)
            | DirectiveMul.NotFollowedBy(Plus | Minus) //- (add | subtract)
            | (add - subtract).Named("DirectiveAdd")
            | subtract.Named("DirectiveSubtract")
        );
        // DirectiveSum.SeparateChildrenBy(SingleLineWhiteSpace);

        
        var greater = DirectiveSum.Then(Greater).Then(DirectiveTest);
        var less = DirectiveSum.Then(Less).Then(DirectiveTest);
        var greaterEqual = DirectiveSum.Then(GreaterEqual).Then(DirectiveTest);
        var lessEqual = DirectiveSum.Then(LessEqual).Then(DirectiveTest);
        
        DirectiveTest.Add(
            ParenDirectiveExpr
            | DirectiveSum.NotFollowedBy(Greater | Less | GreaterEqual | LessEqual)
            | greater.NotFollowedBy(Less | GreaterEqual | LessEqual).SeparatedBy(ls).Named("DirectiveLess")
            | less.NotFollowedBy(GreaterEqual | LessEqual).SeparatedBy(ls).Named("DirectiveGreater")
            | greaterEqual.NotFollowedBy(LessEqual).SeparatedBy(ls).Named("DirectiveLessEqual")
            | lessEqual.SeparatedBy(ls).Named("DirectiveGreaterEqual")
        );
        // DirectiveTest.SeparateChildrenBy(SingleLineWhiteSpace);


        
        ParenDirectiveExpr.Add(
            LeftParen.Then(DirectiveExpr).Then(RightParen).SeparatedBy(ls)
        );

        

        var methodCall = Identifier.Then(LeftParen).Then(RightParen).SeparatedBy(ls).Named("DirectiveMethodCall");

        DirectiveExpr.Add(
            ParenDirectiveExpr
            | DirectiveTest - methodCall      
            | methodCall
        );
        // DirectiveExpr.SeparateChildrenBy(SingleLineWhiteSpace);
    }
}
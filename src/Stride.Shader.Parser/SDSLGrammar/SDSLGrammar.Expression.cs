using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace Stride.Shader.Parser;
public partial class SDSLGrammar : Grammar
{
    public AlternativeParser TermExpression = new();
    public AlternativeParser MulExpression = new();
    public AlternativeParser SumExpression = new();
    public AlternativeParser TestExpression = new();

    public AlternativeParser IncrementExprExpression = new();
    public AlternativeParser ParenExpression = new();
    public AlternativeParser EqualsExpression = new();
    public AlternativeParser PrimaryExpression = new();

    public SDSLGrammar UsingPrimaryExpression()
    {
        Inner = PrimaryExpression;
        return this;
    }

    public void CreateExpressions()
    {
        var ls = SingleLineWhiteSpace.Repeat(0);
        var ls1 = SingleLineWhiteSpace.Repeat(1);

        var incrementOp = 
            PlusPlus 
            | MinusMinus;
        
        var postfixIncrement = 
            Identifier.Then(incrementOp.Named("IncrementOp"));
       
        var prefixIncrement = 
            incrementOp.Named("IncrementOp").Then(Identifier);

        IncrementExprExpression.Add(
            prefixIncrement.SeparatedBy(ls)
            | postfixIncrement.SeparatedBy(ls)
        );
        

        TermExpression.Add(
            Literals
            | Identifier
            | IncrementExprExpression //- (Literals | Identifier).FollowedBy(Literal("==") | "!=")
            | ParenExpression.Named("ParenthesisExpr")
        );

        var multiply = TermExpression.Then(Star).Then(MulExpression).SeparatedBy(ls);
        var divide = TermExpression.Then(Div).Then(MulExpression).SeparatedBy(ls);
        var moderand = TermExpression.Then(Mod).Then(MulExpression).SeparatedBy(ls);

        
        MulExpression.Add(
            TermExpression - (multiply | divide | moderand)
            | (multiply - (divide | moderand)).Named("MultExpression")
            | (divide - moderand).Named("DivExpression")
            | moderand.Named("ModExpression")
            | ParenExpression
        );
        
                
        var add = MulExpression.Then(Plus).Then(SumExpression).SeparatedBy(ls);
        var subtract = MulExpression.Then(Minus).Then(SumExpression).SeparatedBy(ls);
        
        

        SumExpression.Add(
            postfixIncrement.SeparatedBy(ls)
            | MulExpression.NotFollowedBy(Plus | Minus) //- (add | subtract)
            | (add - subtract).Named("AddExpression")
            | subtract.Named("SubtractExpression")
        );
        
        
        var greater = SumExpression.Then(Greater).Then(TestExpression);
        var less = SumExpression.Then(Less).Then(TestExpression);
        var greaterEqual = SumExpression.Then(GreaterEqual).Then(TestExpression);
        var lessEqual = SumExpression.Then(LessEqual).Then(TestExpression);
        
        TestExpression.Add(
            SumExpression.NotFollowedBy(Greater | Less | GreaterEqual | LessEqual)
            | greater.NotFollowedBy(Less | GreaterEqual | LessEqual).SeparatedBy(ls).Named("LessExpression")
            | less.NotFollowedBy(GreaterEqual | LessEqual).SeparatedBy(ls).Named("GreaterExpression")
            | greaterEqual.NotFollowedBy(LessEqual).SeparatedBy(ls).Named("LessEqualExpression")
            | lessEqual.SeparatedBy(ls).Named("GreaterEqualExpression")
        );
        var equals = 
            (BooleanTerm | TermExpression)
            .Then(Literal("==") | "!=")
            .Then(
                BooleanTerm 
                | EqualsExpression
            )
            .SeparatedBy(ls).Named("Equals");
        
        
        EqualsExpression.Add(
            TestExpression - equals
            | equals
        );
        
        ParenExpression.Add(
            LeftParen.Then(ParenExpression).Then(RightParen).SeparatedBy(ls)
            | LeftParen.Then(PrimaryExpression).Then(RightParen).SeparatedBy(ls)
        );

        

        var methodCall = Identifier.Then(LeftParen).Then(RightParen).SeparatedBy(ls).Named("MethodCallExpression");

        PrimaryExpression.Add(
            EqualsExpression
            //     - methodCall      
            // | methodCall
        );
    }
}
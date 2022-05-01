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
    public AlternativeParser IncrementExpression = new();
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
        var ls = WhiteSpace.Repeat(0);
        var ls1 = WhiteSpace.Repeat(1);

        var incrementOp = 
            PlusPlus 
            | MinusMinus;
        
        // Identifier.Then(SingleLineWhiteSpace.Until("++",false,true)).Named("Something");

        var postfixIncrement = 
            Identifier.Then(incrementOp.Named("IncrementOp"));
       
        var prefixIncrement = 
            incrementOp.Named("IncrementOp").Then(Identifier);

        IncrementExpression.Add(
            prefixIncrement.SeparatedBy(ls).Named("PreIncrement")
            | postfixIncrement.SeparatedBy(ls).Named("PostIncrement")
        );

        TermExpression.Add(
            Literals
            | Identifier
            | IncrementExpression - (Literals | Identifier).FollowedBy(Literal("==") | "!=")
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
        );
        
                
        var add = MulExpression.Then(Plus).Then(SumExpression).SeparatedBy(ls);
        var subtract = MulExpression.Then(Minus).Then(SumExpression).SeparatedBy(ls);
        var incrementSum = IncrementExpression.Then(Plus | Minus).Then(SumExpression).SeparatedBy(ls);
        // var postfixAdd = postfixIncrement.Then()
        SumExpression.Add(
            ParenExpression
            | incrementSum 
            | MulExpression.NotFollowedBy(Plus | Minus) //- (add | subtract)
            | (add - (subtract | postfixIncrement)).Named("AddExpression")
            | subtract.Named("SubtractExpression")
        );
        
        
        var greater = SumExpression.Then(Greater).Then(TestExpression);
        var less = SumExpression.Then(Less).Then(TestExpression);
        var greaterEqual = SumExpression.Then(GreaterEqual).Then(TestExpression);
        var lessEqual = SumExpression.Then(LessEqual).Then(TestExpression);
        
        TestExpression.Add(
            ParenExpression
            | SumExpression.NotFollowedBy(Greater | Less | GreaterEqual | LessEqual)
            | greater.NotFollowedBy(Less | GreaterEqual | LessEqual).SeparatedBy(ls).Named("LessExpression")
            | less.NotFollowedBy(GreaterEqual | LessEqual).SeparatedBy(ls).Named("GreaterExpression")
            | greaterEqual.NotFollowedBy(LessEqual).SeparatedBy(ls).Named("LessEqualExpression")
            | lessEqual.SeparatedBy(ls).Named("GreaterEqualExpression")
        );

        var equalsExp = TestExpression.Then(Literal("==").Named("Operator")).Then(BooleanTerm).SeparatedBy(ls);
        var notEqualExp = TestExpression.Then(Literal("!=").Named("Operator")).Then(BooleanTerm).SeparatedBy(ls);
        
        EqualsExpression.Add(
            // ParenExpression
            equalsExp.Named("EqualsExpression")
            // | notEqualExp.Named("NotEqualsExpression")
            // | TestExpression.NotFollowedBy(Literal("==") | "!=")

        );


        
        ParenExpression.Add(
            LeftParen.Then(PrimaryExpression).Then(RightParen)//.SeparatedBy(ls)
        );

        

        // var methodCall = Identifier.Then(LeftParen).Then(RightParen).SeparatedBy(ls).Named("MethodCallExpression");

        PrimaryExpression.Add(
            ParenExpression
            | EqualsExpression      
            // | methodCall
        );

        var assignExpression = 
            Identifier.Then(Equal).Then(PrimaryExpression);

        
        // ExprExpression.SeparateChildrenBy(SingleLineWhiteSpace);
    }
}
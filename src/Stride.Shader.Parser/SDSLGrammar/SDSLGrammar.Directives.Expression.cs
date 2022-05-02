using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace Stride.Shader.Parser;
public partial class SDSLGrammar : Grammar
{
    public AlternativeParser DirectiveTerm = new();
    public AlternativeParser DirectiveMul = new();
    public AlternativeParser DirectiveSum = new();
    public AlternativeParser DirectiveShift = new();
    
    public AlternativeParser DirectiveTest = new();

    public AlternativeParser DirectiveIncrementExpr = new();
    public AlternativeParser ParenDirectiveExpr = new();
    public AlternativeParser DirectiveEquals = new();
    public AlternativeParser DirectiveExpr = new();

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
            Literal("++").Named("PlusPlus")
            | Literal("--").Named("MinusMinus");
        
        var postfixIncrement = 
            Identifier.Then(incrementOp.Repeat(0,1)).SeparatedBy(ls);
        var prefixIncrement = 
            incrementOp.Named("IncrementOp").Then(Identifier).SeparatedBy(ls);



        DirectiveIncrementExpr.Add(
            prefixIncrement.Named("PreIncrement")
            | postfixIncrement
        );
        

        DirectiveTerm.Add(
            Literals
            | IncrementExpression
            | ParenExpression.Named("ParenthesisExpr")
        );

        var multiply = TermExpression.Then(Star | Div | Mod).Then(MulExpression).SeparatedBy(ls);
        DirectiveMul.Add(
            multiply.Named("Multiplication")
            | TermExpression
        );
        
        var sumOp = (Plus - PlusPlus) | (Minus - MinusMinus);
        var add = MulExpression.Then(sumOp).Then(SumExpression).SeparatedBy(ls);
        DirectiveSum.Add(
            add.Named("Addition")
            | MulExpression
        );

        var shiftOp = LeftShift | RightShift;
        var shift = 
            SumExpression.Then(shiftOp.Named("Operator")).Then(ShiftExpression).SeparatedBy(ls);

        DirectiveShift.Add(
            SumExpression
            | shift.Named("ShiftExpression")
        );
        
        
        var testOp = (Less - LeftShift) | LessEqual | (Greater - RightShift) | GreaterEqual;
        var test = ShiftExpression.Then(testOp).Then(TestExpression).SeparatedBy(ls);
        
        DirectiveTest.Add(
            test.Named("TestExpression")
            | ShiftExpression
        );
        var equality = 
            Literal("==").Named("Equals")
            | Literal("!=").Named("NotEquals");
        var equals = 
            (BooleanTerm | TermExpression)
            .Then(equality)
            .Then(
                BooleanTerm 
                | EqualsExpression
            )
            .SeparatedBy(ls).Named("Equals");
        
        
        DirectiveEquals.Add(
            TestExpression
            | equals
        );
        
        ParenDirectiveExpr.Add(
            LeftParen.Then(PrimaryExpression).Then(RightParen).SeparatedBy(ls)
        );

        

        var methodCall = Identifier.Then(LeftParen).Then(RightParen).SeparatedBy(ls).Named("DirectiveMethodCall");

        DirectiveExpr.Add(
            DirectiveEquals
            //     - methodCall      
            // | methodCall
        );
    }
}
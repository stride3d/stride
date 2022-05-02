using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace Stride.Shader.Parser;
public partial class SDSLGrammar : Grammar
{
    public AlternativeParser TermExpression = new();
    public AlternativeParser MulExpression = new();
    public AlternativeParser SumExpression = new();
    public AlternativeParser ShiftExpression = new();

    public AlternativeParser TestExpression = new();

    public AlternativeParser IncrementExpression = new();
    public AlternativeParser ParenExpression = new();
    public AlternativeParser EqualsExpression = new();
    public AlternativeParser PrimaryExpression = new();

    public SDSLGrammar UsingPrimaryExpression()
    {
        Inner = PrimaryExpression.Then(";");
        return this;
    }

    public void CreateExpressions()
    {
        var ls = WhiteSpace.Repeat(0);
        var ls1 = WhiteSpace.Repeat(1);


        var incrementOp = 
            Literal("++").Named("PlusPlus")
            | Literal("--").Named("MinusMinus");
        
        var postfixIncrement = 
            Identifier.Then(incrementOp.Repeat(0,1)).SeparatedBy(ls);
        var prefixIncrement = 
            incrementOp.Named("IncrementOp").Then(Identifier).SeparatedBy(ls);



        IncrementExpression.Add(
            prefixIncrement.Named("PreIncrement")
            | postfixIncrement
        );
        

        TermExpression.Add(
            Literals
            | IncrementExpression
            | ParenExpression.Named("ParenthesisExpr")
        );

        var multiply = TermExpression.Then(Star | Div | Mod).Then(MulExpression).SeparatedBy(ls);
        MulExpression.Add(
            multiply.Named("Multiplication")
            | TermExpression
        );
        var parenMulExpr = 
            LeftParen.Then(MulExpression).Then(RightParen).SeparatedBy(ls);

        
        var sumOp = (Plus - PlusPlus) | (Minus - MinusMinus);
        var add = (parenMulExpr | MulExpression).Then(sumOp).Then(SumExpression).SeparatedBy(ls);
        SumExpression.Add(
            add.Named("Addition")
            | MulExpression
        );

        var parenSumExpr = 
            LeftParen.Then(SumExpression).Then(RightParen).SeparatedBy(ls);

        var shiftOp = LeftShift | RightShift;
        var shift = 
            (parenSumExpr | SumExpression).Then(shiftOp.Named("Operator")).Then(ShiftExpression).SeparatedBy(ls);

        ShiftExpression.Add(
            shift.Named("ShiftExpression")
            | SumExpression 
        );
        
        
        var testOp = Less | LessEqual | Greater | GreaterEqual;
        var test = ShiftExpression.Then(testOp).Then(TestExpression).SeparatedBy(ls);
        
        TestExpression.Add(
            test.Named("TestExpression")
            | ShiftExpression
        );

        var parenTestExpr = LeftParen.Then(TestExpression).Then(RightParen).SeparatedBy(ls);
        
        var eqOp = 
            Literal("==").Named("Equals")
            | Literal("!=").Named("NotEquals");
        
        var equals = 
            (BooleanTerm | parenTestExpr | TestExpression)
            .Then(eqOp)
            .Then(
                BooleanTerm | EqualsExpression
            )
            .SeparatedBy(ls).Named("Equals");
        
        
        EqualsExpression.Add(
            equals
            | TestExpression
        );
        
        ParenExpression.Add(
            LeftParen.Then(PrimaryExpression).Then(RightParen).SeparatedBy(ls)
        );

        
        var parameters = EqualsExpression.Then(Comma.Then(PrimaryExpression).SeparatedBy(ls).Repeat(0)).SeparatedBy(ls);
        var methodCall = Identifier.Then(LeftParen).Then(parameters).Then(RightParen).SeparatedBy(ls).Named("MethodCallExpression");


        PrimaryExpression.Add(
            methodCall
            | EqualsExpression
        );
    }
}
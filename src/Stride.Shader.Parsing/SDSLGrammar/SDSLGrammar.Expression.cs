using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace Stride.Shader.Parsing;
public partial class SDSLGrammar : Grammar
{
    public AlternativeParser TermExpression = new();
    public AlternativeParser PostFixExpression = new();
    public AlternativeParser UnaryExpression = new();
    public AlternativeParser CastExpression = new();
    public AlternativeParser MulExpression = new();
    public AlternativeParser SumExpression = new();
    public AlternativeParser ShiftExpression = new();

    public AlternativeParser ConditionalExpression = new();
    public AlternativeParser LogicalOrExpression = new();
    public AlternativeParser LogicalAndExpression = new();
    public AlternativeParser OrExpression = new();
    public AlternativeParser XorExpression = new();
    public AlternativeParser AndExpression = new();
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
            | Identifier
            | ParenExpression.Named("ParenthesisExpr")
        );

        PostFixExpression.Add(
            Identifier.Then(incrementOp).SeparatedBy(ls).Named("PostfixIncrement")
            | Identifier.NotFollowedBy(ls & LeftBracket).Then(Dot.Then(Identifier).Repeat(0)).Named("AccessorChain")
            | ParenExpression.Or(Identifier).Then(LeftBracket).Then(PrimaryExpression).Then(RightBracket).SeparatedBy(ls).Named("ArrayAccessor")
            | TermExpression
        );

        UnaryExpression.Add(
            Literal("sizeof").Then(LeftParen).Then(Identifier).Then(RightParen).Named("SizeOf")
            | Literal("sizeof").Then(LeftParen).Then(UnaryExpression).Then(RightParen).Named("SizeOf")
            | prefixIncrement.Named("PrefixIncrement")
            | PostFixExpression
        );

        CastExpression.Add(
            LeftParen.Then(Identifier).Then(RightParen).Then(UnaryExpression).SeparatedBy(ls).Named("CastExpression")
            | UnaryExpression
        );

        

        var multiply = CastExpression.Then(Star | Div | Mod).Then(MulExpression).SeparatedBy(ls);
        MulExpression.Add(
            multiply.Named("Multiplication")
            | CastExpression
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
        var parenShift =
            LeftParen.Then(ShiftExpression).Then(RightParen).SeparatedBy(ls);


        
        var testOp = Less | LessEqual | Greater | GreaterEqual;
        var test = (parenShift | ShiftExpression).Then(testOp).Then(TestExpression).SeparatedBy(ls);
        
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

        AndExpression.Add(
            EqualsExpression.Then("&").Then(AndExpression).SeparatedBy(ls).Named("BitwiseAnd")
            | EqualsExpression
        );

        XorExpression.Add(
            AndExpression.Then("^").Then(XorExpression).SeparatedBy(ls).Named("BitwiseXor")
            | AndExpression
        );

        OrExpression.Add(
            XorExpression.Then("|").Then(OrExpression).SeparatedBy(ls).Named("BitwiseOr")
            | XorExpression
        );

        LogicalAndExpression.Add(
            OrExpression.Then("&&").Then(LogicalAndExpression).SeparatedBy(ls).Named("LogicalAnd")
            | OrExpression
        );
        LogicalOrExpression.Add(
            LogicalAndExpression.Then("||").Then(LogicalOrExpression).SeparatedBy(ls).Named("LogicalOr")
            | LogicalAndExpression
        );

        ConditionalExpression.Add( 
            LogicalOrExpression.NotFollowedBy(ls & "?")
            | LogicalOrExpression
                .Then("?")
                    .Then(CastExpression | ParenExpression | LogicalOrExpression)
                    .Then(":")
                    .Then(CastExpression | ParenExpression | LogicalOrExpression)
                    .SeparatedBy(ls)
                    .Named("Ternary")
                
        );
        
        ParenExpression.Add(
            LeftParen.Then(PrimaryExpression).Then(RightParen).SeparatedBy(ls)
        );

        
        var parameters = EqualsExpression.Then(Comma.Then(PrimaryExpression).SeparatedBy(ls).Repeat(0)).SeparatedBy(ls);
        var methodCall = Identifier.Then(LeftParen).Then(parameters).Then(RightParen).SeparatedBy(ls).Named("MethodCallExpression");


        PrimaryExpression.Add(
            methodCall
            | ConditionalExpression
        );
    }
}
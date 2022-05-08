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
    public AlternativeParser MethodCall = new();
    public AlternativeParser PrimaryExpression = new();

    public SDSLGrammar UsingPrimaryExpression()
    {
        Inner = PostFixExpression;
        return this;
    }

    public void CreateExpressions()
    {
        var ws = WhiteSpace.Repeat(0);
        var ls1 = WhiteSpace.Repeat(1);


        var incrementOp = 
            PlusPlus
            | MinusMinus;
        

        TermExpression.Add(
            Literals,
            Identifier,
            ParenExpression.Named("ParenthesisExpr")
        );
        
        var arrayAccess = new SequenceParser();
        var chain = new SequenceParser();
        var postfixInc = new SequenceParser();


        arrayAccess.Add(Identifier, LeftBracket, PrimaryExpression, RightBracket);
        chain.Add(
            arrayAccess.Named("ArrayAccessor") | Identifier,
            (Dot & (arrayAccess.Named("ArrayAccessor") | Identifier)).Repeat(1)
        );        
        postfixInc.Add(
            chain.Named("AccessorChain") | arrayAccess.Named("ArrayAccessor") | Identifier, 
            incrementOp.Named("Operator")
        );

        PostFixExpression.Add(
            arrayAccess.SeparateChildrenBy(ws).Named("ArrayAccessor").NotFollowedBy(Dot | (ws & incrementOp)),
            chain.Named("ChainAccessor").NotFollowedBy(ws & incrementOp),
            postfixInc.Named("PostFixIncrement"),
            TermExpression
        );

        UnaryExpression.Add(
            (incrementOp & ws & Identifier).Named("PrefixIncrement"),
            Literal("sizeof").Then(LeftParen).Then(Identifier | UnaryExpression).Then(RightParen).Named("SizeOf"),
            // Literal("sizeof").Then(LeftParen).Then(UnaryExpression).Then(RightParen).Named("SizeOf"),
            PostFixExpression
        );

        CastExpression.Add(
            LeftParen.Then(Identifier).Then(RightParen).Then(UnaryExpression).SeparatedBy(ws).Named("CastExpression")
            | UnaryExpression
        );

        

        var multiply = CastExpression.Then(Star | Div | Mod).Then(MulExpression).SeparatedBy(ws);
        MulExpression.Add(
            multiply.Named("Multiplication")
            | TermExpression
        );
        var parenMulExpr = 
            LeftParen.Then(MulExpression).Then(RightParen).SeparatedBy(ws);

        
        var sumOp = (Plus - PlusPlus) | (Minus - MinusMinus);
        var add = (parenMulExpr | MulExpression).Then(sumOp).Then(SumExpression).SeparatedBy(ws);
        SumExpression.Add(
            add.Named("Addition")
            | MulExpression
        );

        var parenSumExpr = 
            LeftParen.Then(SumExpression).Then(RightParen).SeparatedBy(ws);

        var shiftOp = LeftShift | RightShift;
        var shift = 
            (parenSumExpr | SumExpression).Then(shiftOp.Named("Operator")).Then(ShiftExpression).SeparatedBy(ws);

        ShiftExpression.Add(
            TermExpression.NotFollowedBy(ws & shiftOp)
            | shift.Named("ShiftExpression")
            // TermExpression.Then(RightShift).Then(TermExpression).SeparatedBy(ws)
        );
        var parenShift =
            LeftParen.Then(ShiftExpression).Then(RightParen).SeparatedBy(ws);


        
        var testOp = Less | LessEqual | Greater | GreaterEqual;
        var test = (parenShift | ShiftExpression).Then(testOp).Then(TestExpression).SeparatedBy(ws);
        
        TestExpression.Add(
            test.Named("TestExpression")
            | ShiftExpression
        );

        var parenTestExpr = LeftParen.Then(TestExpression).Then(RightParen).SeparatedBy(ws);
        
        var eqOp = 
            Literal("==").Named("Equals")
            | Literal("!=").Named("NotEquals");
        
        var equals = 
            (BooleanTerm | parenTestExpr | TestExpression)
            .Then(eqOp)
            .Then(
                BooleanTerm | EqualsExpression
            )
            .SeparatedBy(ws).Named("Equals");
        
        
        EqualsExpression.Add(
            equals
            | TestExpression
        );

        AndExpression.Add(
            EqualsExpression.Then("&").Then(AndExpression).SeparatedBy(ws).Named("BitwiseAnd")
            | EqualsExpression
        );

        XorExpression.Add(
            AndExpression.Then("^").Then(XorExpression).SeparatedBy(ws).Named("BitwiseXor")
            | AndExpression
        );

        OrExpression.Add(
            XorExpression.Then("|").Then(OrExpression).SeparatedBy(ws).Named("BitwiseOr")
            | XorExpression
        );

        LogicalAndExpression.Add(
            OrExpression.Then("&&").Then(LogicalAndExpression).SeparatedBy(ws).Named("LogicalAnd")
            | OrExpression
        );
        LogicalOrExpression.Add(
            LogicalAndExpression.Then("||").Then(LogicalOrExpression).SeparatedBy(ws).Named("LogicalOr")
            | LogicalAndExpression
        );

        ConditionalExpression.Add( 
            LogicalOrExpression.NotFollowedBy(ws & "?")
            | LogicalOrExpression
                .Then("?")
                    .Then(CastExpression | ParenExpression | LogicalOrExpression)
                    .Then(":")
                    .Then(CastExpression | ParenExpression | LogicalOrExpression)
                    .SeparatedBy(ws)
                    .Named("Ternary")
                
        );
        
        ParenExpression.Add(
            LeftParen.Then(PrimaryExpression).Then(RightParen).SeparatedBy(ws)
        );

        
        var parameters = EqualsExpression.Then(Comma.Then(PrimaryExpression).SeparatedBy(ws).Repeat(0)).SeparatedBy(ws);
        MethodCall.Add(
            Identifier.Then(LeftParen).Then(parameters).Then(RightParen).SeparatedBy(ws).Named("MethodCallExpression")
        );


        PrimaryExpression.Add(
            MethodCall
            | ConditionalExpression
        );
    }
}
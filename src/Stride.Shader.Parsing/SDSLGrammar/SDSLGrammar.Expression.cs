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
        Inner = SumExpression.Then(";");
        return this;
    }

    public void CreateExpressions()
    {
        var ws = WhiteSpace.Repeat(0);
        var ls1 = WhiteSpace.Repeat(1);


        var incrementOp = new AlternativeParser();
        incrementOp.Add(
            PlusPlus,
            MinusMinus
        );
        

        TermExpression.Add(
            Literals,
            Identifier.Except(Keywords | ValueTypes)
            // ,ParenExpression
        );
        
        var arrayAccess = new SequenceParser();
        var chain = new SequenceParser();
        var postfixInc = new SequenceParser();


        arrayAccess.Add(
            Identifier,
            ws,
            (LeftBracket & ws & PrimaryExpression & ws & RightBracket).Repeat(1).SeparatedBy(ws)
        );
        chain.Add(
            arrayAccess.Named("ArrayAccessor") | Identifier,
            ws,
            (Dot & ws & (arrayAccess.Named("ArrayAccessor") | Identifier)).Repeat(1)
        );        
        postfixInc.Add(
            chain.Named("AccessorChain") | arrayAccess.Named("ArrayAccessor") | Identifier, 
            ws,
            incrementOp.Named("Operator")
        );

        PostFixExpression.Add(
            TermExpression.NotFollowedBy(ws & (Dot | LeftBracket | incrementOp)),
            postfixInc.Named("PostfixIncrement"),
            chain.Named("AccessorChain"),
            arrayAccess.Named("ArrayAccesor")
        );

        var prefixInc = new SequenceParser();
        prefixInc.Add(
            incrementOp,
            ws,
            TermExpression.NotFollowedBy(ws & (Dot | "["))
            | chain
            | arrayAccess
        );

        UnaryExpression.Add(
            PostFixExpression,
            prefixInc.Named("PrefixIncrement"),
            Literal("sizeof").Then(LeftParen).Then(Identifier | UnaryExpression).Then(RightParen).Named("SizeOf")
        );

        var cast = new SequenceParser();
        cast.Add(
            LeftParen,
            ValueTypes | Identifier,
            RightParen,
            UnaryExpression
        );

        CastExpression.Add(
            UnaryExpression,
            cast.SeparatedBy(ws).Named("CastExpression")
        );

        
        var mulOp = Star | Div | Mod;
        var multiply = new SequenceParser();
        multiply.Add(
            CastExpression,
            mulOp.Named("Operator"),
            MulExpression
        );
        
        MulExpression.Add(
            CastExpression.NotFollowedBy(ws & mulOp),
            multiply.SeparatedBy(ws).Named("Multiplication")
        );
        var parenMulExpr = 
            LeftParen.Then(MulExpression).Then(RightParen).SeparatedBy(ws);

        
        var sumOp = new AlternativeParser();
        sumOp.Add(Plus, Minus);
        
        var add = new SequenceParser();
        add.Add(
            parenMulExpr.NotFollowedBy(UnaryExpression) | MulExpression,
            ws,
            sumOp.Except(incrementOp),
            ws,
            SumExpression
        );
        
        
        SumExpression.Add( 
            MulExpression.NotFollowedBy(ws & sumOp.Except(incrementOp)),
            add.Named("Addition")
        );

        var parenSumExpr = 
            LeftParen.Then(SumExpression).Then(RightParen).SeparatedBy(ws);

        var shiftOp = (LeftShift | RightShift);
        var shift = new SequenceParser();
        shift.Add(
            parenSumExpr.NotFollowedBy(UnaryExpression) | SumExpression,
            ws,
            shiftOp.Named("Operator"),
            ws,
            ShiftExpression
        );

        ShiftExpression.Add(
            SumExpression.NotFollowedBy(ws & shiftOp),
            shift.Named("ShiftExpression")
        );
        var parenShift =
            LeftParen.Then(ShiftExpression).Then(RightParen).SeparatedBy(ws);


        
        var testOp = Less | LessEqual | Greater | GreaterEqual;
        var test = new SequenceParser();
        test.Add(
            parenShift | ShiftExpression,
            testOp.Named("Operator"),
            TestExpression
        );
        
        TestExpression.Add(
            test.Named("TestExpression"),
            ShiftExpression
        );

        var parenTestExpr = LeftParen.Then(TestExpression).Then(RightParen).SeparatedBy(ws);
        
        var eqOp = 
            Literal("==").Named("Equals")
            | Literal("!=").Named("NotEquals");
        
        var equals = new SequenceParser();
        equals.Add(
            BooleanTerm | TestExpression,
            ws,
            eqOp.Named("Operator"),
            ws,
            BooleanTerm | EqualsExpression
        );
        
        EqualsExpression.Add(
            equals.Named("EqualExpression"),
            TestExpression
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
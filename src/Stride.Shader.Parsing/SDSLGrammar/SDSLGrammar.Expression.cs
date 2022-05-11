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
    public SequenceParser MethodCall = new();
    public AlternativeParser PrimaryExpression = new();

    public SDSLGrammar UsingPrimaryExpression()
    {
        Inner = SumExpression.Then(";");
        return this;
    }

    public Parser Parenthesis(Parser p, bool notFollowedByUnary = true)
    {
        if (notFollowedByUnary)
            return LeftParen.Then(p).Then(RightParen).SeparatedBy(WhiteSpace.Repeat(0)).NotFollowedBy(UnaryExpression);
        else
            return LeftParen.Then(p).Then(RightParen).SeparatedBy(WhiteSpace.Repeat(0));
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

        // TODO : write tests for method calls
        // TODO : Optimize method call
        

        TermExpression.Add(
            Literals,
            Identifier.Except(Keywords | ValueTypes).NotFollowedBy(ws & LeftParen),
            MethodCall
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
            Identifier.NotFollowedBy(ws & (Dot | "["))
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
        
        var sumOp = new AlternativeParser();
        sumOp.Add(Plus, Minus);
        
        var add = new SequenceParser();
        add.Add(
            Parenthesis(MulExpression) | MulExpression,
            ws,
            sumOp.Except(incrementOp),
            ws,
            SumExpression
        );
        
        
        SumExpression.Add( 
            MulExpression.NotFollowedBy(ws & sumOp.Except(incrementOp)),
            add.Named("Addition")
        );

       
        var shiftOp = (LeftShift | RightShift);
        var shift = new SequenceParser();
        shift.Add(
            Parenthesis(SumExpression) | SumExpression,
            ws,
            shiftOp.Named("Operator"),
            ws,
            ShiftExpression
        );

        ShiftExpression.Add(
            SumExpression.NotFollowedBy(ws & shiftOp),
            shift.Named("ShiftExpression")
        );
        

        AndExpression.Add(
            ShiftExpression.NotFollowedBy(ws & And.Except(AndAnd)),
            (Parenthesis(ShiftExpression) | ShiftExpression).Then(And).Then(AndExpression).SeparatedBy(ws).Named("BitwiseAnd")
        );

        XorExpression.Add(
            AndExpression.NotFollowedBy(ws & "^"),
            (Parenthesis(AndExpression) | AndExpression).Then("^").Then(XorExpression).SeparatedBy(ws).Named("BitwiseXor")
        );


        OrExpression.Add(
            XorExpression.NotFollowedBy(ws & Or.Except(OrOr)),
            (Parenthesis(XorExpression) | XorExpression).Then(Or).Then(OrExpression).SeparatedBy(ws).Named("BitwiseOr")
        );

        var testOp = Less | LessEqual | Greater | GreaterEqual;
        var test = new SequenceParser();
        test.Add(
            Parenthesis(OrExpression) | OrExpression,
            ws,
            testOp.Named("Operator"),
            ws,
            TestExpression
        );

        TestExpression.Add(
            OrExpression.NotFollowedBy(ws & testOp),
            test.Named("TestExpression")
        );

        var eqOp =
            Literal("==")
            | Literal("!=");

        var equals = new SequenceParser();
        equals.Add(
            Parenthesis(TestExpression) | TestExpression | BooleanTerm,
            ws,
            eqOp.Named("Operator"),
            ws,
            EqualsExpression | BooleanTerm
        );

        EqualsExpression.Add(
            TestExpression.NotFollowedBy(ws & eqOp),
            equals.Named("EqualExpression")
        );

        LogicalAndExpression.Add(
            EqualsExpression.NotFollowedBy(ws & AndAnd),
            (Parenthesis(EqualsExpression) | EqualsExpression).Then(AndAnd).Then(LogicalAndExpression).SeparatedBy(ws).Named("LogicalAnd")
        );
        LogicalOrExpression.Add(
            LogicalAndExpression.NotFollowedBy(ws & OrOr),
            (Parenthesis(LogicalAndExpression) | LogicalAndExpression).Then(OrOr).Then(LogicalOrExpression).SeparatedBy(ws).Named("LogicalOr")
        );

        ConditionalExpression.Add( 
            LogicalOrExpression.NotFollowedBy(ws & "?")
            | (Parenthesis(LogicalOrExpression) | LogicalOrExpression)
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
        
        var parameters =
            PrimaryExpression.Repeat(0).SeparatedBy(ws & Comma & ws);

        MethodCall.Add(
            Identifier.Then(LeftParen).Then(parameters).Then(RightParen).SeparatedBy(ws).Named("MethodCallExpression")
        );


        PrimaryExpression.Add(
            MethodCall,
            ConditionalExpression            
        );
    }
}
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
        MulExpression.Add(
            CastExpression.Repeat(0).SeparatedBy(ws & mulOp.Named("Operator") & ws)
        );
        
        var sumOp = Plus | Minus;        
        
        SumExpression.Add( 
            (Parenthesis(MulExpression) | MulExpression)
                .Repeat(0).SeparatedBy(ws & sumOp.Named("Operator") & ws)
        );

       
        var shiftOp = LeftShift | RightShift;

        ShiftExpression.Add(
            (Parenthesis(SumExpression) | SumExpression)
                .Repeat(0).SeparatedBy(ws & shiftOp.Named("Operator") & ws)
        );
        

        AndExpression.Add(
            (Parenthesis(ShiftExpression) | ShiftExpression)
                .Repeat(0).SeparatedBy(ws & And.Named("Operator") & ws)
        );

        XorExpression.Add(
            (Parenthesis(AndExpression) | AndExpression)
                .Repeat(0).SeparatedBy(ws & Literal("^").Named("Operator") & ws)
        );


        OrExpression.Add(
            (Parenthesis(XorExpression) | XorExpression)
                .Repeat(0).SeparatedBy(ws & Or.Named("Operator") & ws)
        );

        var testOp = Less | LessEqual | Greater | GreaterEqual;

        TestExpression.Add(
            (Parenthesis(OrExpression) | OrExpression)
                .Repeat(0).SeparatedBy(ws & testOp.Named("Operator") & ws)
        );

        var eqOp =
            Literal("==")
            | Literal("!=");

        EqualsExpression.Add(
            (Parenthesis(TestExpression) | TestExpression)
                .Repeat(0).SeparatedBy(ws & eqOp.Named("Operator") & ws)
        );

        LogicalAndExpression.Add(
            (Parenthesis(EqualsExpression) | EqualsExpression)
                .Repeat(0).SeparatedBy(ws & AndAnd.Named("Operator") & ws)
        );
        LogicalOrExpression.Add(
            (Parenthesis(LogicalAndExpression) | LogicalAndExpression)
                .Repeat(0).SeparatedBy(ws & OrOr.Named("Operator") & ws)
        );

        ConditionalExpression.Add( 
            LogicalOrExpression.NotFollowedBy(ws & "?"),
            (Parenthesis(LogicalOrExpression) | LogicalOrExpression)
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
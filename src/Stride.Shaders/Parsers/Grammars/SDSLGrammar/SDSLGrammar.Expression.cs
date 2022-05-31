using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace Stride.Shaders.Parsing.Grammars.SDSL;
public partial class SDSLGrammar : Grammar
{
    public AlternativeParser TermExpression = new(){Name = "TermExpression"};
    public AlternativeParser PostfixExpression = new(){Name = "PostfixExpression"};
    public AlternativeParser UnaryExpression = new(){Name = "UnaryExpression"};
    public AlternativeParser CastExpression = new(){Name = "CastExpression"};
    public AlternativeParser MulExpression = new(){Name = "MulExpression"};
    public AlternativeParser SumExpression = new(){Name = "SumExpression"};
    public AlternativeParser ShiftExpression = new(){Name = "ShiftExpression"};

    public AlternativeParser ConditionalExpression = new() { Name = "ConditionalExpression" };
    public AlternativeParser LogicalOrExpression = new(){Name = "LogicalOrExpression"};
    public AlternativeParser LogicalAndExpression = new(){Name = "LogicalAndExpression"};
    public AlternativeParser OrExpression = new(){Name = "OrExpression"};
    public AlternativeParser XorExpression = new(){Name = "XorExpression"};
    public AlternativeParser AndExpression = new(){Name = "AndExpression"};
    public AlternativeParser TestExpression = new(){Name = "TestExpression"};

    public AlternativeParser IncrementExpression = new() { Name = "IncrementExpression" };
    public AlternativeParser ParenExpression = new(){Name = "ParenExpression"};
    public AlternativeParser EqualsExpression = new(){Name = "EqualsExpression"};
    public SequenceParser MethodCall = new(){Name = "MethodCall"};
    public AlternativeParser PrimaryExpression = new(){Name = "PrimaryExpression"};

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
    public Parser Parenthesis(Parser p, Parser f, bool notFollowedByUnary = true)
    {
        var ws = WhiteSpace.Repeat(0);
        if (notFollowedByUnary)
            return LeftParen.Then(p).Then(RightParen).SeparatedBy(WhiteSpace.Repeat(0)).NotFollowedBy(UnaryExpression).FollowedBy(ws & f);
        else
            return LeftParen.Then(p).Then(RightParen).SeparatedBy(WhiteSpace.Repeat(0)).FollowedBy(ws & f);
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
            Identifier.Named("VariableTerm").Except(Keywords | ValueTypes).NotFollowedBy(ws & LeftParen).Named("VariableTerm"),
            MethodCall,
            Parenthesis(PrimaryExpression)
        );
        
        var arrayAccess = new SequenceParser() { Name = "ArrayAccessor"};
        var chain = new SequenceParser() { Name = "ChainAccessor"};
        var postfixInc = new SequenceParser() { Name = "PostfixIncrement"};


        arrayAccess.Add(
            Identifier,
            ws,
            (LeftBracket & PrimaryExpression & RightBracket)
                .SeparatedBy(ws)
                .Repeat(1)
                .SeparatedBy(ws)
        );
        chain.Add(
            (arrayAccess | MethodCall | Identifier).Repeat(1).SeparatedBy(ws & Dot & ws)
        );
        postfixInc.Add(
            chain | arrayAccess | Identifier, 
            ws,
            incrementOp.Named("Operator")
        );

        PostfixExpression.Add(
            TermExpression.NotFollowedBy(ws & (Dot | LeftBracket | incrementOp)),
            postfixInc,
            chain,
            arrayAccess
        );

        var prefixInc = new SequenceParser(
            incrementOp.Named("Operator"),
            ws,
            Identifier.NotFollowedBy(ws & (Dot | "["))
            | chain
            | arrayAccess
        )
        { Name = "PrefixIncrement"};

        UnaryExpression.Add(
            PostfixExpression,
            (Plus | Minus) & ws & PostfixExpression,
            prefixInc,
            Literal("sizeof").Then(LeftParen).Then(Identifier | UnaryExpression).Then(RightParen).Named("SizeOf")
        );

        var cast = new SequenceParser(
            LeftParen,
            ValueTypes | Identifier.Named("TypeName"),
            RightParen,
            UnaryExpression
        )
        { Name = "CastExpression", Separator = ws};

        CastExpression.Add(
            UnaryExpression,
            cast
        );

        
        var mulOp = Star | Div | Mod;  
        MulExpression.Add(
            (Parenthesis(PrimaryExpression) | CastExpression).Repeat(0).SeparatedBy(ws & mulOp.Named("Operator") & ws)
        );
        
        var sumOp = Plus | Minus;        
        
        SumExpression.Add(
            MulExpression.Repeat(0).SeparatedBy(ws & sumOp.Named("Operator") & ws),
            Parenthesis(PrimaryExpression).Repeat(0).SeparatedBy(ws & sumOp.Named("Operator") & ws)
        );

       
        var shiftOp = LeftShift | RightShift;

        ShiftExpression.Add(
            SumExpression.Repeat(0).SeparatedBy(ws & shiftOp.Named("Operator") & ws),
            Parenthesis(PrimaryExpression).Repeat(0).SeparatedBy(ws & shiftOp.Named("Operator") & ws)
        );
        

        AndExpression.Add(
            ShiftExpression.Repeat(0).SeparatedBy(ws & And.Named("Operator") & ws),
            Parenthesis(PrimaryExpression).Repeat(0).SeparatedBy(ws & And.Named("Operator") & ws)
        );

        XorExpression.Add(
            AndExpression.Repeat(0).SeparatedBy(ws & Literal("^").Named("Operator") & ws),
            Parenthesis(PrimaryExpression).Repeat(0).SeparatedBy(ws & Literal("^").Named("Operator") & ws)
        );


        OrExpression.Add(
            XorExpression.Repeat(0).SeparatedBy(ws & Or.Named("Operator") & ws),
            Parenthesis(PrimaryExpression).Repeat(0).SeparatedBy(ws & Or.Named("Operator") & ws)
        );

        var testOp = LessEqual | Less | GreaterEqual | Greater ;

        TestExpression.Add(
            OrExpression.Repeat(0).SeparatedBy(ws & testOp.Named("Operator") & ws),
            Parenthesis(PrimaryExpression).Repeat(0).SeparatedBy(ws & testOp.Named("Operator") & ws)
        );

        var eqOp =
            Literal("==")
            | Literal("!=");

        EqualsExpression.Add(
            TestExpression.Repeat(0).SeparatedBy(ws & eqOp.Named("Operator") & ws),
            Parenthesis(PrimaryExpression).Repeat(0).SeparatedBy(ws & eqOp.Named("Operator") & ws)
        );

        LogicalAndExpression.Add(
            EqualsExpression.Repeat(0).SeparatedBy(ws & AndAnd.Named("Operator") & ws),
            Parenthesis(PrimaryExpression).Repeat(0).SeparatedBy(ws & AndAnd.Named("Operator") & ws)
        );
        LogicalOrExpression.Add(
            LogicalAndExpression.Repeat(0).SeparatedBy(ws & OrOr.Named("Operator") & ws),
            Parenthesis(PrimaryExpression).Repeat(0).SeparatedBy(ws & OrOr.Named("Operator") & ws)
        );

        var ternary = new SequenceParser(
            Parenthesis(LogicalOrExpression) | LogicalOrExpression,
            Question,
            Parenthesis(LogicalOrExpression) | LogicalOrExpression,
            Colon,
            Parenthesis(LogicalOrExpression) | LogicalOrExpression
        ){ Separator = ws, Name = "Ternary"};


        ConditionalExpression.Add(
            LogicalOrExpression.NotFollowedBy(ws & "?"),
            ternary
        );
        
        ParenExpression.Add(
            LeftParen.Then(PrimaryExpression).Then(RightParen).SeparatedBy(ws)
        );
        

        MethodCall.Add(
            Identifier,
            LeftParen,
            PrimaryExpression.Repeat(0).SeparatedBy(ws & Comma & ws).Until(RightParen),
            RightParen
        );
        MethodCall.Separator = ws;

        var arrayDeclaration =
            (LeftBrace & PrimaryExpression.Repeat(0).SeparatedBy(ws & Comma & ws) & RightBrace)
            .SeparatedBy(ws);

        PrimaryExpression.Add(
            arrayDeclaration,
            // MethodCall,
            ConditionalExpression            
        );
    }
}
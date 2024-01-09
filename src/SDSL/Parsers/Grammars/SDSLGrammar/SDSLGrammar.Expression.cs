using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;
using static SDSL.Parsing.Grammars.CommonParsers;

namespace SDSL.Parsing.Grammars.SDSL;
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
    public SequenceParser ValueTypesMethods = new(){Name = "ValueTypesMethods"};
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
            return LeftParen.Then(p).Then(RightParen).SeparatedBy(Spaces).NotFollowedBy(UnaryExpression);
        else
            return LeftParen.Then(p).Then(RightParen).SeparatedBy(Spaces);
    }
    public Parser Parenthesis(Parser p, Parser f, bool notFollowedByUnary = true)
    {
        if (notFollowedByUnary)
            return LeftParen.Then(p).Then(RightParen).SeparatedBy(Spaces).NotFollowedBy(UnaryExpression).FollowedBy(Spaces & f);
        else
            return LeftParen.Then(p).Then(RightParen).SeparatedBy(Spaces).FollowedBy(Spaces & f);
    }

    public void CreateExpressions()
    {


        var incrementOp = new AlternativeParser();
        incrementOp.Add(
            PlusPlus,
            MinusMinus
        );
        
        ValueTypesMethods.Add(
            ValueTypes,
            LeftParen,
            PrimaryExpression.Repeat(0).SeparatedBy(Spaces & Comma & Spaces).Until(RightParen),
            RightParen
        );
        ValueTypesMethods.Separator = Spaces;

        TermExpression.Add(
            Literals,
            Identifier.Named("VariableTerm").Except(Keywords | SimpleTypes).NotFollowedBy(Spaces & LeftParen).Named("VariableTerm"),
            ValueTypesMethods.Named("ValueTypesMethod"),
            MethodCall,
            Parenthesis(PrimaryExpression)
        );
        
        var arrayAccess = new SequenceParser() { Name = "ArrayAccessor"};
        var chain = new SequenceParser() { Name = "ChainAccessor"};
        var postfixInc = new SequenceParser() { Name = "PostfixIncrement"};


        arrayAccess.Add(
            Identifier,
            Spaces,
            (LeftBracket & PrimaryExpression & RightBracket)
                .SeparatedBy(Spaces)
                .Repeat(1)
                .SeparatedBy(Spaces)
        );
        chain.Add(
            (arrayAccess | MethodCall | Identifier).Repeat(1).SeparatedBy(Spaces & Dot & Spaces)
        );
        postfixInc.Add(
            chain | arrayAccess | Identifier, 
            Spaces,
            incrementOp.Named("Operator")
        );

        PostfixExpression.Add(
            TermExpression.NotFollowedBy(Spaces & (Dot | LeftBracket | incrementOp)),
            postfixInc,
            chain,
            arrayAccess
        );

        var prefixInc = new SequenceParser(
            incrementOp.Named("Operator"),
            Spaces,
            Identifier.NotFollowedBy(Spaces & (Dot | "["))
            | chain
            | arrayAccess
        )
        { Name = "PrefixIncrement"};

        UnaryExpression.Add(
            PostfixExpression,
            (Plus | Minus) & Spaces & PostfixExpression,
            prefixInc,
            Literal("sizeof").Then(LeftParen).Then(Identifier | UnaryExpression).Then(RightParen).Named("SizeOf")
        );

        var cast = new SequenceParser(
            LeftParen,
            SimpleTypes | Identifier.Named("TypeName"),
            RightParen,
            UnaryExpression
        )
        { Name = "CastExpression", Separator = Spaces};

        CastExpression.Add(
            UnaryExpression,
            cast
        );

        
        var mulOp = Star | Div | Mod;  
        MulExpression.Add(
            (Parenthesis(PrimaryExpression) | CastExpression).Repeat(0).SeparatedBy(Spaces & mulOp.Named("Operator") & Spaces)
        );
        
        var sumOp = Plus | Minus;        
        
        SumExpression.Add(
            MulExpression.Repeat(0).SeparatedBy(Spaces & sumOp.Named("Operator") & Spaces),
            Parenthesis(PrimaryExpression).Repeat(0).SeparatedBy(Spaces & sumOp.Named("Operator") & Spaces)
        );

       
        var shiftOp = LeftShift | RightShift;

        ShiftExpression.Add(
            SumExpression.Repeat(0).SeparatedBy(Spaces & shiftOp.Named("Operator") & Spaces),
            Parenthesis(PrimaryExpression).Repeat(0).SeparatedBy(Spaces & shiftOp.Named("Operator") & Spaces)
        );
        

        AndExpression.Add(
            ShiftExpression.Repeat(0).SeparatedBy(Spaces & And.Named("Operator") & Spaces),
            Parenthesis(PrimaryExpression).Repeat(0).SeparatedBy(Spaces & And.Named("Operator") & Spaces)
        );

        XorExpression.Add(
            AndExpression.Repeat(0).SeparatedBy(Spaces & Literal("^").Named("Operator") & Spaces),
            Parenthesis(PrimaryExpression).Repeat(0).SeparatedBy(Spaces & Literal("^").Named("Operator") & Spaces)
        );


        OrExpression.Add(
            XorExpression.Repeat(0).SeparatedBy(Spaces & Or.Named("Operator") & Spaces),
            Parenthesis(PrimaryExpression).Repeat(0).SeparatedBy(Spaces & Or.Named("Operator") & Spaces)
        );

        var testOp = LessEqual | Less | GreaterEqual | Greater ;

        TestExpression.Add(
            OrExpression.Repeat(0).SeparatedBy(Spaces & testOp.Named("Operator") & Spaces),
            Parenthesis(PrimaryExpression).Repeat(0).SeparatedBy(Spaces & testOp.Named("Operator") & Spaces)
        );

        var eqOp =
            Literal("==")
            | Literal("!=");

        EqualsExpression.Add(
            TestExpression.Repeat(0).SeparatedBy(Spaces & eqOp.Named("Operator") & Spaces),
            Parenthesis(PrimaryExpression).Repeat(0).SeparatedBy(Spaces & eqOp.Named("Operator") & Spaces)
        );

        LogicalAndExpression.Add(
            EqualsExpression.Repeat(0).SeparatedBy(Spaces & AndAnd.Named("Operator") & Spaces),
            Parenthesis(PrimaryExpression).Repeat(0).SeparatedBy(Spaces & AndAnd.Named("Operator") & Spaces)
        );
        LogicalOrExpression.Add(
            LogicalAndExpression.Repeat(0).SeparatedBy(Spaces & OrOr.Named("Operator") & Spaces),
            Parenthesis(PrimaryExpression).Repeat(0).SeparatedBy(Spaces & OrOr.Named("Operator") & Spaces)
        );

        var ternary = new SequenceParser(
            Parenthesis(LogicalOrExpression) | LogicalOrExpression,
            Question,
            Parenthesis(LogicalOrExpression) | LogicalOrExpression,
            Colon,
            Parenthesis(LogicalOrExpression) | LogicalOrExpression
        ){ Separator = Spaces, Name = "Ternary"};


        ConditionalExpression.Add(
            LogicalOrExpression.NotFollowedBy(Spaces & "?"),
            ternary
        );
        
        ParenExpression.Add(
            LeftParen.Then(PrimaryExpression).Then(RightParen).SeparatedBy(Spaces)
        );
        

        MethodCall.Add(
            Identifier,
            LeftParen,
            PrimaryExpression.Repeat(0).SeparatedBy(Spaces & Comma & Spaces).Until(RightParen),
            RightParen
        );
        MethodCall.Separator = Spaces;

        var arrayDeclaration =
            (LeftBrace & PrimaryExpression.Repeat(0).SeparatedBy(Spaces & Comma & Spaces) & RightBrace)
            .SeparatedBy(Spaces);

        PrimaryExpression.Add(
            arrayDeclaration,
            // MethodCall,
            ConditionalExpression            
        );
    }
}
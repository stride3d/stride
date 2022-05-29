using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace Stride.Shader.Parsing.Grammars.SDSL;
public partial class SDSLGrammar : Grammar
{
    public AlternativeParser DirectiveTermExpression = new() { Name = "DirectiveTermExpression" };
    public AlternativeParser DirectivePostFixExpression = new() { Name = "DirectivePostFixExpression" };
    public AlternativeParser DirectiveUnaryExpression = new() { Name = "DirectiveUnaryExpression" };
    public AlternativeParser DirectiveCastExpression = new() { Name = "DirectiveCastExpression" };
    public AlternativeParser DirectiveMulExpression = new() { Name = "DirectiveMulExpression" };
    public AlternativeParser DirectiveSumExpression = new() { Name = "DirectiveSumExpression" };
    public AlternativeParser DirectiveShiftExpression = new() { Name = "DirectiveShiftExpression" };

    public AlternativeParser DirectiveConditionalExpression = new() { Name = "DirectiveConditionalExpression" };
    public AlternativeParser DirectiveLogicalOrExpression = new() { Name = "DirectiveLogicalOrExpression" };
    public AlternativeParser DirectiveLogicalAndExpression = new() { Name = "DirectiveLogicalAndExpression" };
    public AlternativeParser DirectiveOrExpression = new() { Name = "DirectiveOrExpression" };
    public AlternativeParser DirectiveXorExpression = new() { Name = "DirectiveXorExpression" };
    public AlternativeParser DirectiveAndExpression = new() { Name = "DirectiveAndExpression" };
    public AlternativeParser DirectiveTestExpression = new() { Name = "DirectiveTestExpression" };

    public AlternativeParser DirectiveIncrementExpression = new() { Name = "DirectiveIncrementExpression" };
    public AlternativeParser DirectiveParenExpression = new() { Name = "DirectiveParenExpression" };
    public AlternativeParser DirectiveEqualsExpression = new() { Name = "DirectiveEqualsExpression" };
    public SequenceParser DirectivesMethodCall = new() { Name = "DirectivesMethodCall" };
    public AlternativeParser DirectiveExpression = new() { Name = "DirectiveExpression" };
    public SDSLGrammar DirectiveUsingDirectiveExpression()
    {
        Inner = DirectiveExpression;
        return this;
    }
    public void CreateDirectiveExpressions()
    {
        var ws = SingleLineWhiteSpace.Repeat(0);
        var ls1 = SingleLineWhiteSpace.Repeat(1);



        var incrementOp = new AlternativeParser();
        incrementOp.Add(
            PlusPlus,
            MinusMinus
        );

        // TODO : write tests for method calls
        // TODO : Optimize method call


        DirectiveTermExpression.Add(
            Literals,
            ~(Plus | Minus & ws) & Identifier.Except(Keywords | ValueTypes).NotFollowedBy(ws & LeftParen),
            DirectivesMethodCall,
            Parenthesis(DirectiveExpression)
        );

        var arrayAccess = new SequenceParser();
        var chain = new SequenceParser();
        var postfixInc = new SequenceParser();


        arrayAccess.Add(
            Identifier,
            ws,
            (LeftBracket & DirectiveExpression & RightBracket)
                .SeparatedBy(ws)
                .Repeat(1)
                .SeparatedBy(ws)
        );
        chain.Add(
            (arrayAccess | MethodCall | Identifier).Repeat(1).SeparatedBy(ws & Dot & ws)
        );
        postfixInc.Add(
            chain.Named("AccessorChain") | arrayAccess.Named("ArrayAccessor") | Identifier,
            ws,
            incrementOp.Named("Operator")
        );

        DirectivePostFixExpression.Add(
            DirectiveTermExpression.NotFollowedBy(ws & (Dot | LeftBracket | incrementOp)),
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

        DirectiveUnaryExpression.Add(
            DirectivePostFixExpression,
            prefixInc.Named("PrefixIncrement"),
            Literal("sizeof").Then(LeftParen).Then(Identifier | DirectiveUnaryExpression).Then(RightParen).Named("SizeOf")
        );

        var cast = new SequenceParser();
        cast.Add(
            LeftParen,
            ValueTypes | Identifier,
            RightParen,
            DirectiveUnaryExpression
        );

        DirectiveCastExpression.Add(
            DirectiveUnaryExpression,
            cast.SeparatedBy(ws).Named("DirectiveCastExpression")
        );


        var mulOp = Star | Div | Mod;
        DirectiveMulExpression.Add(
            (Parenthesis(DirectiveExpression) | DirectiveCastExpression).Repeat(0).SeparatedBy(ws & mulOp.Named("Operator") & ws)
        );

        var sumOp = Plus | Minus;

        DirectiveSumExpression.Add(
            DirectiveMulExpression.Repeat(0).SeparatedBy(ws & sumOp.Named("Operator") & ws),
            Parenthesis(DirectiveExpression).Repeat(0).SeparatedBy(ws & sumOp.Named("Operator") & ws)
        );


        var shiftOp = LeftShift | RightShift;

        DirectiveShiftExpression.Add(
            DirectiveSumExpression.Repeat(0).SeparatedBy(ws & shiftOp.Named("Operator") & ws),
            Parenthesis(DirectiveExpression).Repeat(0).SeparatedBy(ws & shiftOp.Named("Operator") & ws)
        );


        DirectiveAndExpression.Add(
            DirectiveShiftExpression.Repeat(0).SeparatedBy(ws & And.Named("Operator") & ws),
            Parenthesis(DirectiveExpression).Repeat(0).SeparatedBy(ws & And.Named("Operator") & ws)
        );

        DirectiveXorExpression.Add(
            DirectiveAndExpression.Repeat(0).SeparatedBy(ws & Literal("^").Named("Operator") & ws),
            Parenthesis(DirectiveExpression).Repeat(0).SeparatedBy(ws & Literal("^").Named("Operator") & ws)
        );


        DirectiveOrExpression.Add(
            DirectiveXorExpression.Repeat(0).SeparatedBy(ws & Or.Named("Operator") & ws),
            Parenthesis(DirectiveExpression).Repeat(0).SeparatedBy(ws & Or.Named("Operator") & ws)
        );

        var testOp = LessEqual | Less | GreaterEqual | Greater;

        DirectiveTestExpression.Add(
            DirectiveOrExpression.Repeat(0).SeparatedBy(ws & testOp.Named("Operator") & ws),
            Parenthesis(DirectiveExpression).Repeat(0).SeparatedBy(ws & testOp.Named("Operator") & ws)
        );

        var eqOp =
            Literal("==")
            | Literal("!=");

        DirectiveEqualsExpression.Add(
            DirectiveTestExpression.Repeat(0).SeparatedBy(ws & eqOp.Named("Operator") & ws),
            Parenthesis(DirectiveExpression).Repeat(0).SeparatedBy(ws & eqOp.Named("Operator") & ws)
        );

        DirectiveLogicalAndExpression.Add(
            DirectiveEqualsExpression.Repeat(0).SeparatedBy(ws & AndAnd.Named("Operator") & ws),
            Parenthesis(DirectiveExpression).Repeat(0).SeparatedBy(ws & AndAnd.Named("Operator") & ws)
        );
        DirectiveLogicalOrExpression.Add(
            DirectiveLogicalAndExpression.Repeat(0).SeparatedBy(ws & OrOr.Named("Operator") & ws),
            Parenthesis(DirectiveExpression).Repeat(0).SeparatedBy(ws & OrOr.Named("Operator") & ws)
        );

        DirectiveConditionalExpression.Add(
            DirectiveLogicalOrExpression.NotFollowedBy(ws & "?"),
            (Parenthesis(DirectiveLogicalOrExpression).NotFollowedBy(ws & OrOr) | DirectiveLogicalOrExpression)
                .Then("?")
                    .Then(DirectiveCastExpression | DirectiveParenExpression | DirectiveLogicalOrExpression)
                    .Then(":")
                    .Then(DirectiveCastExpression | DirectiveParenExpression | DirectiveLogicalOrExpression)
                    .SeparatedBy(ws)
                    .Named("Ternary")

        );

        DirectiveParenExpression.Add(
            LeftParen.Then(DirectiveExpression).Then(RightParen).SeparatedBy(ws)
        );

        var parameters =
            DirectiveExpression.Repeat(0).SeparatedBy(ws & Comma & ws);

        DirectivesMethodCall.Add(
            Identifier.Then(LeftParen).Then(parameters).Then(RightParen).SeparatedBy(ws).Named("DirectiveMethodCallExpression")
        );

        var arrayDeclaration =
            (LeftBrace & DirectiveExpression.Repeat(0).SeparatedBy(ws & Comma & ws) & RightBrace)
            .SeparatedBy(ws);

        DirectiveExpression.Add(
            arrayDeclaration,
            DirectiveConditionalExpression
        );
    }
}
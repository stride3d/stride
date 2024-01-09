using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;
using static SDSL.Parsing.Grammars.CommonParsers;

namespace SDSL.Parsing.Grammars.SDSL;
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

        var incrementOp = new AlternativeParser();
        incrementOp.Add(
            PlusPlus,
            MinusMinus
        );

        // TODO : write tests for method calls
        // TODO : Optimize method call


        DirectiveTermExpression.Add(
            Literals,
            ~(Plus | Minus & Spaces) & Identifier.Except(Keywords | SimpleTypes).NotFollowedBy(Spaces & LeftParen),
            DirectivesMethodCall,
            Parenthesis(DirectiveExpression)
        );

        var arrayAccess = new SequenceParser();
        var chain = new SequenceParser();
        var postfixInc = new SequenceParser();


        arrayAccess.Add(
            Identifier,
            Spaces,
            (LeftBracket & DirectiveExpression & RightBracket)
                .SeparatedBy(Spaces)
                .Repeat(1)
                .SeparatedBy(Spaces)
        );
        chain.Add(
            (arrayAccess | MethodCall | Identifier).Repeat(1).SeparatedBy(Spaces & Dot & Spaces)
        );
        postfixInc.Add(
            chain.Named("AccessorChain") | arrayAccess.Named("ArrayAccessor") | Identifier,
            Spaces,
            incrementOp.Named("Operator")
        );

        DirectivePostFixExpression.Add(
            DirectiveTermExpression.NotFollowedBy(Spaces & (Dot | LeftBracket | incrementOp)),
            postfixInc.Named("PostfixIncrement"),
            chain.Named("AccessorChain"),
            arrayAccess.Named("ArrayAccesor")
        );

        var prefixInc = new SequenceParser();
        prefixInc.Add(
            incrementOp,
            Spaces,
            Identifier.NotFollowedBy(Spaces & (Dot | "["))
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
            SimpleTypes | Identifier,
            RightParen,
            DirectiveUnaryExpression
        );

        DirectiveCastExpression.Add(
            DirectiveUnaryExpression,
            cast.SeparatedBy(Spaces).Named("DirectiveCastExpression")
        );


        var mulOp = Star | Div | Mod;
        DirectiveMulExpression.Add(
            (Parenthesis(DirectiveExpression) | DirectiveCastExpression).Repeat(0).SeparatedBy(Spaces & mulOp.Named("Operator") & Spaces)
        );

        var sumOp = Plus | Minus;

        DirectiveSumExpression.Add(
            DirectiveMulExpression.Repeat(0).SeparatedBy(Spaces & sumOp.Named("Operator") & Spaces),
            Parenthesis(DirectiveExpression).Repeat(0).SeparatedBy(Spaces & sumOp.Named("Operator") & Spaces)
        );


        var shiftOp = LeftShift | RightShift;

        DirectiveShiftExpression.Add(
            DirectiveSumExpression.Repeat(0).SeparatedBy(Spaces & shiftOp.Named("Operator") & Spaces),
            Parenthesis(DirectiveExpression).Repeat(0).SeparatedBy(Spaces & shiftOp.Named("Operator") & Spaces)
        );


        DirectiveAndExpression.Add(
            DirectiveShiftExpression.Repeat(0).SeparatedBy(Spaces & And.Named("Operator") & Spaces),
            Parenthesis(DirectiveExpression).Repeat(0).SeparatedBy(Spaces & And.Named("Operator") & Spaces)
        );

        DirectiveXorExpression.Add(
            DirectiveAndExpression.Repeat(0).SeparatedBy(Spaces & Literal("^").Named("Operator") & Spaces),
            Parenthesis(DirectiveExpression).Repeat(0).SeparatedBy(Spaces & Literal("^").Named("Operator") & Spaces)
        );


        DirectiveOrExpression.Add(
            DirectiveXorExpression.Repeat(0).SeparatedBy(Spaces & Or.Named("Operator") & Spaces),
            Parenthesis(DirectiveExpression).Repeat(0).SeparatedBy(Spaces & Or.Named("Operator") & Spaces)
        );

        var testOp = LessEqual | Less | GreaterEqual | Greater;

        DirectiveTestExpression.Add(
            DirectiveOrExpression.Repeat(0).SeparatedBy(Spaces & testOp.Named("Operator") & Spaces),
            Parenthesis(DirectiveExpression).Repeat(0).SeparatedBy(Spaces & testOp.Named("Operator") & Spaces)
        );

        var eqOp =
            Literal("==")
            | Literal("!=");

        DirectiveEqualsExpression.Add(
            DirectiveTestExpression.Repeat(0).SeparatedBy(Spaces & eqOp.Named("Operator") & Spaces),
            Parenthesis(DirectiveExpression).Repeat(0).SeparatedBy(Spaces & eqOp.Named("Operator") & Spaces)
        );

        DirectiveLogicalAndExpression.Add(
            DirectiveEqualsExpression.Repeat(0).SeparatedBy(Spaces & AndAnd.Named("Operator") & Spaces),
            Parenthesis(DirectiveExpression).Repeat(0).SeparatedBy(Spaces & AndAnd.Named("Operator") & Spaces)
        );
        DirectiveLogicalOrExpression.Add(
            DirectiveLogicalAndExpression.Repeat(0).SeparatedBy(Spaces & OrOr.Named("Operator") & Spaces),
            Parenthesis(DirectiveExpression).Repeat(0).SeparatedBy(Spaces & OrOr.Named("Operator") & Spaces)
        );

        DirectiveConditionalExpression.Add(
            DirectiveLogicalOrExpression.NotFollowedBy(Spaces & "?"),
            (Parenthesis(DirectiveLogicalOrExpression).NotFollowedBy(Spaces & OrOr) | DirectiveLogicalOrExpression)
                .Then("?")
                    .Then(DirectiveCastExpression | DirectiveParenExpression | DirectiveLogicalOrExpression)
                    .Then(":")
                    .Then(DirectiveCastExpression | DirectiveParenExpression | DirectiveLogicalOrExpression)
                    .SeparatedBy(Spaces)
                    .Named("Ternary")

        );

        DirectiveParenExpression.Add(
            LeftParen.Then(DirectiveExpression).Then(RightParen).SeparatedBy(Spaces)
        );

        var parameters =
            DirectiveExpression.Repeat(0).SeparatedBy(Spaces & Comma & Spaces);

        DirectivesMethodCall.Add(
            Identifier.Then(LeftParen).Then(parameters).Then(RightParen).SeparatedBy(Spaces).Named("DirectiveMethodCallExpression")
        );

        var arrayDeclaration =
            (LeftBrace & DirectiveExpression.Repeat(0).SeparatedBy(Spaces & Comma & Spaces) & RightBrace)
            .SeparatedBy(Spaces);

        DirectiveExpression.Add(
            arrayDeclaration,
            DirectiveConditionalExpression
        );
    }
}
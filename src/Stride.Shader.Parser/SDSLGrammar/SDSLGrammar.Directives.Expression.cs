using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace Stride.Shader.Parser;
public partial class SDSLGrammar : Grammar
{
    public AlternativeParser DirectiveTerm = new();
    public AlternativeParser DirectiveMul = new();
    public AlternativeParser DirectiveSum = new();
    public AlternativeParser DirectiveTest = new();



    public SDSLGrammar UsingDirectiveExpression()
    {
        Inner = DirectiveTest;
        return this;
    }

    public void CreateDirectiveExpressions()
    {
        DirectiveTerm =
            Literals
            | Identifier
            | StringLiteral;
        
        var mulOp = Star | Div | Mod;

        var multiply = DirectiveTerm.Then(Star).Then(DirectiveMul).Named("DirectiveMultiply");
        var divide = DirectiveTerm.Then(Div).Then(DirectiveMul).Named("DirectiveDivide");
        var moderand = DirectiveTerm.Then(Mod).Then(DirectiveMul).Named("DirectiveModerand");


        
        DirectiveMul.Add(
            DirectiveTerm - (multiply | divide | moderand)
            | multiply - (divide | moderand)
            | divide - moderand
            | moderand
        );
        
        
        // DirectiveMul.Add(multiply);
        
        var add = DirectiveMul.Then(Plus).Then(DirectiveSum).Named("DirectiveAdd");
        var subtract = DirectiveMul.Then(Minus).Then(DirectiveSum).Named("DirectiveSubtract");
        
        DirectiveSum.Add(
            DirectiveMul - (add | subtract)
            | add - subtract
            | subtract
        );
        
        var greater = DirectiveSum.Then(Greater).Then(DirectiveTest).Named("DirectiveGreater");
        var less = DirectiveSum.Then(Less).Then(DirectiveTest).Named("DirectiveLess");
        var greaterEqual = DirectiveSum.Then(GreaterEqual).Then(DirectiveTest).Named("DirectiveGreaterEqual");
        var lessEqual = DirectiveSum.Then(LessEqual).Then(DirectiveTest).Named("DirectiveLessEqual");
        
        DirectiveTest.Add(
            DirectiveSum - (greater | less | greaterEqual | lessEqual)
            | greater - (less | greaterEqual | lessEqual)
            | less - (greaterEqual | lessEqual)
            | greaterEqual - lessEqual
            | lessEqual
        );

            //(DirectiveTerm.Except(multiply)).Or(multiply);
            // | divide
            // | mod;
    }
}
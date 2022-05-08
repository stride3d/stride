using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace Stride.Shader.Parsing;
public partial class SDSLGrammar : Grammar
{
    public AlternativeParser ShaderValueDeclaration = new();
    public AlternativeParser ConstantBufferValueDeclaration = new();
    public SDSLGrammar UsingDeclarators()
    {
        var ws  = WhiteSpace.Repeat(0);
        Inner = ShaderValueDeclaration & Semi;
        // Inner = Identifier.Then(LeftBracket).Then(PrimaryExpression).Then(RightBracket).Then(";");
        return this;
    }
    public void CreateDeclarators()
    {
        var ws = WhiteSpace.Repeat(0);
        var ws1 = WhiteSpace.Repeat(1);

        var declare =
            Identifier.Then(Identifier).SeparatedBy(ws1).Then(Semi).SeparatedBy(ws);

        var declaratorSupplement =
            Colon.Then(
                    Packoffset.Then(LeftParen).Then(Identifier.Then(Dot.Then(Identifier).Repeat(0))).Then(RightParen).SeparatedBy(ws).Named("PackOffset")
                    | Register.Then(LeftParen).Then(Identifier.Then(Comma.Then(Identifier).SeparatedBy(ws).Repeat(0).SeparatedBy(ws))).Then(RightParen).SeparatedBy(ws).Named("RegisterAllocation")
                    | Identifier.Named("Semantic")
                ).SeparatedBy(ws).Optional();

        var supplement =
            (
                Colon &
                (
                    (Packoffset & LeftParen & Identifier & ((Dot & Identifier).Repeat(0)) & RightParen)
                        .SeparatedBy(ws).Named("PackOffset")
                    | (Register & LeftParen & (Identifier & (Comma & Identifier).SeparatedBy(ws)).Repeat(0).SeparateChildrenBy(ws) & RightParen)
                        .SeparatedBy(ws).Named("Register")
                    | Identifier.Named("Semantic")
                )
            ).SeparatedBy(ws);

        var valueDeclaration =
            (~Stage & ~Stream & ValueTypes & Identifier).SeparatedBy(ws1)
            & (~(LeftBracket & Literals & RightBracket).SeparatedBy(ws));
        


        ShaderValueDeclaration.Add(
            (valueDeclaration & ~(AssignOperators & PrimaryExpression))
            .SeparatedBy(ws)
        );
        ConstantBufferValueDeclaration.Add(
            (valueDeclaration & ~(AssignOperators & PrimaryExpression) & ~supplement)
            .SeparatedBy(ws)
        );
        StructDefinition.Add(
            Struct.Then(Identifier).SeparatedBy(ws1)
            .Then(LeftBrace)
                .Then(declare.Repeat(0).SeparatedBy(ws))
            .Then(RightBrace).Then(Semi).SeparatedBy(ws)
        );
    }
}
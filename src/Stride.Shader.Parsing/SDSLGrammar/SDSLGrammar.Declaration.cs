using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace Stride.Shader.Parsing;
public partial class SDSLGrammar : Grammar
{
    public SequenceParser ShaderValueDeclaration = new();
    public SequenceParser ConstantBufferValueDeclaration = new();
    public SequenceParser StructDefinition = new();

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

        var packoffset = new SequenceParser();
        packoffset.Add(
            Packoffset,
            LeftParen,
            Identifier,
            (Dot & Identifier).Repeat(0),
            RightParen
        );
        packoffset.Separator = ws;

        var register = new SequenceParser();
        register.Add(
            Register,
            LeftParen,
            (Identifier & ~Comma).SeparatedBy(ws).Repeat(0).SeparatedBy(ws),
            RightParen
        );
        register.Separator = ws;

        var supplement = new SequenceParser();
        supplement.Add(
            Colon,
            ws,
            packoffset.Named("PackOffset")
            | register
            | Identifier.Named("Semantic")
        );

        var staging =
            Stage
            | Stage & ws1 & Stream
            | Stream;

        var valueDeclaration = new SequenceParser();
        valueDeclaration.Add(
            staging.Then(ws1).Optional(),
            ValueTypes | Identifier,
            ws1,
            Identifier
        );

        var assignOrSupplement =
            (AssignOperators & PrimaryExpression & supplement).SeparatedBy(ws)
            | (AssignOperators & PrimaryExpression).SeparatedBy(ws)
            | supplement;

        ShaderValueDeclaration.Add(
            valueDeclaration,
            assignOrSupplement.Optional(),
            Semi
        );
        ShaderValueDeclaration.Separator = ws;

        ConstantBufferValueDeclaration.Add(
            valueDeclaration,
            assignOrSupplement.Optional(),
            Semi
        );

        ConstantBufferValueDeclaration.Separator = ws;

        StructDefinition.Add(
            Struct & ws1 & Identifier,
            LeftBrace,
            (declare & Semi).SeparatedBy(ws).Repeat(0).SeparatedBy(ws),
            RightBrace,
            Semi
        );
        StructDefinition.Separator = ws;
    }
}
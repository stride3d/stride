using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace Stride.Shader.Parsing;
public partial class SDSLGrammar : Grammar
{
    public SequenceParser IfDirective = new();
    public SequenceParser ElseDirective = new();
    public SequenceParser ElifDirective = new();
    public SequenceParser DefineDirective = new();
    public SequenceParser EndIfDirective = new();
    

    public SequenceParser IfDefDirective = new();
    public SequenceParser IfNDefDirective = new();

    public SequenceParser ConditionalDirectives = new();
    public SequenceParser DefineDirectives = new();

    public SequenceParser Directives = new();

    public SDSLGrammar UsingDirectives()
    {
        Inner = Directives;
        return this;
    }
    public void CreateDirectives()
    {
        var ls = SingleLineWhiteSpace.Repeat(0);
        var ls1 = SingleLineWhiteSpace.Repeat(1);
        var hash = Literal("#");
        var hashIfNDef = Literal("#ifndef").Named("hashifndef");
        var hashIfDef = Literal("#ifdef").Named("hashifdef");
        var hashIf = Literal("#if").Named("hashif");
        var hashEndIf = Literal("#endif").Named("HashEndIf");
        var hashElse = Literal("#else").Named("HashElse");
        var hashElif = Literal("#elif").Named("HashElif");
        var hashDefine = Literal("#define").Named("HashElif");

        IfDirective.Add(hashIf, ls1, DirectiveExpression);
        ElseDirective.Add(hashElse, ls, Eol);
        EndIfDirective.Add(hashEndIf, ls, Eol);
        IfDefDirective.Add(hashIfDef, ls1, Identifier, ls, Eol);
        IfNDefDirective.Add(hashIfNDef, ls1, Identifier, ls, Eol);
        DefineDirective.Add(hashDefine, ls1, Identifier, ls1, DirectiveExpression, ls, Eol);

        var anyChars = AnyChar.Repeat(0);

        var elseList = new AlternativeParser(
            (ElifDirective & anyChars.Until(hashElif | hashElse | hashEndIf).Named("ElifCode")).Repeat(),
            ElseDirective & anyChars.Until(hashEndIf).Named("ElseCode")
        );

        ConditionalDirectives.Add(
            IfDirective,
            anyChars.Until(hashElif | hashElse | hashEndIf).Named("IfCode"),
            ~elseList,
            EndIfDirective
        );
        DefineDirective.Add(
            IfDefDirective | IfNDefDirective,
            ConditionalDirectives | DefineDirective | anyChars.Repeat(0).Until(hashElse | hashEndIf),
            ~(ElseDirective & anyChars.Repeat().Until(EndIfDirective)),
            EndIfDirective
        );
        Directives.Add(
            anyChars.Until(hashIf | hashIfDef | hashIfNDef).Named("UnchangedCode"),
            (
                DefineDirective 
                | ConditionalDirectives 
                | anyChars.Until(hashIf | hashIfDef | hashIfNDef | End).Named("UnchangedCode")
            )
            .Repeat(0)
        );
    }
}
using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace Stride.Shader.Parsing.Grammars.Directive;
public partial class DirectiveGrammar : Grammar
{
    public SequenceParser IfDirective = new(){Name = "IfDirective"};
    public SequenceParser ElseDirective = new() { Name = "ElseDirective" };
    public SequenceParser ElifDirective = new(){Name = "ElifDirective"};
    public SequenceParser DefineDirective = new(){Name = "DefineDirective"};
    public SequenceParser EndIfDirective = new(){Name = "EndIfDirective"};
    

    public SequenceParser IfDefDirective = new() { Name = "IfDefDirective" };
    public SequenceParser IfNDefDirective = new(){Name = "IfNDefDirective"};

    public SequenceParser ConditionalDirectives = new(){Name = "ConditionalDirectives"};
    public SequenceParser DefineDirectives = new(){Name = "DefineDirectives"};

    public SequenceParser Directives = new();

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

        IfDirective.Add(hashIf, ls1, DirectiveExpression, ls.Until(Eol | End, true));
        ElseDirective.Add(hashElse, ls.Until(Eol | End, true));
        ElifDirective.Add(hashElif, ls1, DirectiveExpression, ls.Until(Eol | End, true));
        EndIfDirective.Add(hashEndIf, ls.Until(Eol | End, true));
        IfDefDirective.Add(hashIfDef, ls1, Identifier, ls.Until(Eol | End, true));
        IfNDefDirective.Add(hashIfNDef, ls1, Identifier, ls.Until(Eol | End, true));
        DefineDirective.Add(hashDefine, ls1, Identifier, ls1, DirectiveExpression, ls.Until(Eol | End, true));

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
        DefineDirectives.Add(
            IfDefDirective | IfNDefDirective,
            ConditionalDirectives | DefineDirective | anyChars.Repeat(0).Until(hashElse | hashEndIf),
            ~(ElseDirective & anyChars.Repeat().Until(EndIfDirective)),
            EndIfDirective
        );

        var ifDefCode = (IfDefDirective | IfNDefDirective) & AnyChar.Repeat(0).Until(hashDefine | hashIf | hashElse | hashEndIf).Named("IfDefCode");
        var elseCode = ElseDirective & AnyChar.Repeat(0).Until(hashEndIf).Named("ElseCode");
        var ifCode = IfDirective & AnyChar.Repeat(0).Until(hashElif | hashElse | hashEndIf).Named("IfCode");
        var elifCode = ElifDirective & AnyChar.Repeat(0).Until(hashElif | hashElse).Named("ElifCode");

        var conditional = ifCode & elifCode.Repeat(0).Until(hashEndIf | hashElse) & ~elseCode & EndIfDirective;
        var definition = new SequenceParser(
            ifDefCode,
            (conditional.Named("Conditional") | AnyChar.Repeat(0).Until(hashEndIf | hashIf).Named("IfDefCode")).Repeat(0).Until(hashElse | hashEndIf),
            ~elseCode.Named("ElseIfDef"),
            EndIfDirective
        );


        Directives.Add(
            AnyChar.Repeat(0).Until(End | hashIf | hashIfDef | hashIfNDef | hashDefine).Named("UnchangedCode"),
            (definition.Named("IfDefinition") | conditional.Named("Conditional") | DefineDirective | AnyChar.Repeat(0).Until(End | hashIf | hashIfDef | hashIfNDef | hashDefine).Named("UnchangedCode"))
                .Repeat(0).Until(End)
        );
    }
}
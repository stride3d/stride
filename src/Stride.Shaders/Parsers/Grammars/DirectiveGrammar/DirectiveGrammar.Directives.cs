using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace Stride.Shaders.Parsing.Grammars.Directive;
public partial class DirectiveGrammar : Grammar
{
    public SequenceParser IfDirective = new(){Name = "IfDirective"};
    public SequenceParser ElseDirective = new() { Name = "ElseDirective" };
    public SequenceParser ElifDirective = new(){Name = "ElifDirective"};
    public SequenceParser DefineDirective = new(){Name = "DefineDirective"};
    public SequenceParser EndIfDirective = new(){Name = "EndIfDirective"};
    

    public SequenceParser IfDefDirective = new() { Name = "IfDefDirective" };
    public SequenceParser IfNDefDirective = new(){Name = "IfNDefDirective"};

    public SequenceParser IfDefCode = new() { Name = "IfDefCode" };
    public SequenceParser ElseCode = new() { Name = "ElseCode" };
    public SequenceParser IfCode = new() { Name = "IfCode" };
    public SequenceParser ElifCode = new() { Name = "ElifCode" };


    public SequenceParser ConditionalDirectives = new(){Name = "ConditionalDirectives"};
    public SequenceParser DefinitionDirectives = new(){Name = "DefineDirectives"};
    public AlternativeParser AnyDirectives = new();

    public SequenceParser Directives = new();

    public void CreateDirectives()
    {
        var ls = SingleLineWhiteSpace.Repeat(0);
        var ls1 = SingleLineWhiteSpace.Repeat(1);
        var ws = WhiteSpace.Repeat(0);

        var hash = Literal("#");
        var hashIfNDef = Literal("#ifndef").Named("ifndef");
        var hashIfDef = Literal("#ifdef").Named("ifdef");
        var hashIf = Literal("#if").Named("if");
        var hashEndIf = Literal("#endif").Named("endif");
        var hashElse = Literal("#else").Named("else");
        var hashElif = Literal("#elif").Named("elif");
        var hashDefine = Literal("#define").Named("define");

        var startHash =
            hashIfNDef
            | hashIfDef
            | hashIf
            | hashDefine;

        var anyHash =
            startHash
            | hashElif
            | hashElse
            | hashEndIf;

        IfDirective.Add(hashIf, ls1, DirectiveExpression, ls.Until(Eol | End, true));
        ElseDirective.Add(hashElse, ls.Until(Eol | End, true));
        ElifDirective.Add(hashElif, ls1, DirectiveExpression, ls.Until(Eol | End, true));
        EndIfDirective.Add(hashEndIf, ls.Until(Eol | End, true));
        IfDefDirective.Add(hashIfDef, ls1, Identifier, ls.Until(Eol | End, true));
        IfNDefDirective.Add(hashIfNDef, ls1, Identifier, ls.Until(Eol | End, true));
        DefineDirective.Add(hashDefine, ls1, Identifier, ~(ls1 & DirectiveExpression), ls.Until(Eol | End));

        

        var CodeOrDirective =
            AnyDirectives
            .Or(AnyChar.Repeat(0).Until(startHash | End).Named("CodeSnippet"))
            .Repeat(1).Until(End);

        IfDefCode.Add(
            IfDefDirective | IfNDefDirective,
            AnyDirectives.Or(AnyChar.Repeat(0).Until(startHash).Named("CodeSnippet"))
            .Repeat(0).Until(hashElse | hashEndIf).Named("Children"),
            EndIfDirective | ElseCode & EndIfDirective
        );

        ElseCode.Add(
            ElseDirective,
            AnyDirectives
                .Or(AnyChar.Repeat(0).Until(startHash).Named("CodeSnippet"))
                .Repeat(0).Until(hashEndIf).Named("Children")
        );

        IfCode.Add(
            IfDirective,
            AnyDirectives.Or(AnyChar.Repeat(0).Until(anyHash).Named("CodeSnippet"))
            .Repeat(0).Until(hashElif | hashElse | hashEndIf).Named("Children"),
            ElifCode.Repeat(0),
            EndIfDirective | ElseCode & EndIfDirective
        );

        ElifCode.Add(
            ElifDirective,
            AnyDirectives.Or(AnyChar.Repeat(0).Until(startHash).Named("CodeSnippet"))
            .Repeat(0).Until(hashElse | hashEndIf).Named("Children")
        );

        AnyDirectives.Add(
            DefineDirective,
            IfDefCode,
            IfCode
        );

        Directives.Add(
            CodeOrDirective.Until(End).Named("Directives")
        );
        Inner = Directives;
    }
}
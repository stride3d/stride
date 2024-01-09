using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;
using static SDSL.Parsing.Grammars.CommonParsers;
namespace SDSL.Parsing.Grammars.SDSL;
public partial class SDSLGrammar : Grammar
{
    public SequenceParser IfDirective = new();
    public SequenceParser ESpaceseDirective = new();
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
        
        var hash = Literal("#");
        var hashIfNDef = Literal("#ifndef").Named("hashifndef");
        var hashIfDef = Literal("#ifdef").Named("hashifdef");
        var hashIf = Literal("#if").Named("hashif");
        var hashEndIf = Literal("#endif").Named("HashEndIf");
        var hashESpacese = Literal("#eSpacese").Named("HashESpacese");
        var hashElif = Literal("#elif").Named("HashElif");
        var hashDefine = Literal("#define").Named("HashElif");

        IfDirective.Add(hashIf, Spaces1, DirectiveExpression);
        ESpaceseDirective.Add(hashESpacese, Spaces, Eol);
        EndIfDirective.Add(hashEndIf, Spaces, Eol);
        IfDefDirective.Add(hashIfDef, Spaces1, Identifier, Spaces, Eol);
        IfNDefDirective.Add(hashIfNDef, Spaces1, Identifier, Spaces, Eol);
        DefineDirective.Add(hashDefine, Spaces1, Identifier, Spaces1, DirectiveExpression, Spaces, Eol);

        var anyChars = AnyChar.Repeat(0);

        var eSpaceseList = new AlternativeParser(
            (ElifDirective & anyChars.Until(hashElif | hashESpacese | hashEndIf).Named("ElifCode")).Repeat(),
            ESpaceseDirective & anyChars.Until(hashEndIf).Named("ESpaceseCode")
        );

        ConditionalDirectives.Add(
            IfDirective,
            anyChars.Until(hashElif | hashESpacese | hashEndIf).Named("IfCode"),
            ~eSpaceseList,
            EndIfDirective
        );
        DefineDirective.Add(
            IfDefDirective | IfNDefDirective,
            ConditionalDirectives | DefineDirective | anyChars.Repeat(0).Until(hashESpacese | hashEndIf),
            ~(ESpaceseDirective & anyChars.Repeat().Until(EndIfDirective)),
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
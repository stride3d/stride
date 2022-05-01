using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace Stride.Shader.Parser;
public partial class SDSLGrammar : Grammar
{
    public SequenceParser IfDefDirective = new();
    public SequenceParser IfNDefDirective = new();

    public AlternativeParser Directives = new();

    public SDSLGrammar UsingIfDefDirective()
    {
        Inner = Directives;
        return this;
    }
    public void CreateDirectives()
    {
        var ls = SingleLineWhiteSpace.Repeat(0);
        var ls1 = SingleLineWhiteSpace.Repeat(1);
        var hash = Literal("#");
        var hashIfNDef = Literal("ifndef").Named("hashifndef");
        var hashIfDef = Literal("ifdef").Named("hashifdef");
        var hashIf = Literal("if").Named("hashif");
        var hashEndIf = Literal("endif").Named("HashEndIf");
        var hashElse = Literal("else").Named("HashElse");
        var hashElif = Literal("elif").Named("HashElif");
        var hashDefine = Literal("define").Named("HashElif");

        // TODO : add if and elif
        Directives.Add(
            hash
            .Then(
                hashElse.Then(ls).Then(LetterOrDigit.Or(Punctuation).Not()).Named("DirectiveElse")
                | hashEndIf.Then(ls).Then(LetterOrDigit.Or(Punctuation).Not()).Named("DirectiveEnd")
                | (hashIfDef - (hashIfNDef | hashIf | hashEndIf)).Then(Identifier).SeparatedBy(ls1).Named("DirectiveIfDef")
                | (hashIfNDef - (hashIf | hashEndIf)).Then(Identifier).SeparatedBy(ls1).Named("DirectiveIfNDef")
                | hashDefine.Then(Identifier).Then(Literals).SeparatedBy(ls1).Named("DirectiveDefine")
            )
        );
    }
}
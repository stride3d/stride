using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace Stride.Shader.Parser;
public partial class SDSLGrammar : Grammar
{
    public SequenceParser IfDirective = new();
    public SequenceParser ElseDirective = new();
    public SequenceParser ElifDirective = new();
    public SequenceParser DefineDirective = new();
    public SequenceParser EndIfDirective = new();
    

    public SequenceParser IfDefDirective = new();
    public SequenceParser IfNDefDirective = new();

    public AlternativeParser Directives = new();

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
        var hashIfNDef = Literal("ifndef").Named("hashifndef");
        var hashIfDef = Literal("ifdef").Named("hashifdef");
        var hashIf = Literal("if").Named("hashif");
        var hashEndIf = Literal("endif").Named("HashEndIf");
        var hashElse = Literal("else").Named("HashElse");
        var hashElif = Literal("elif").Named("HashElif");
        var hashDefine = Literal("define").Named("HashElif");

        IfDirective = hashIf.Then(DirectiveExpr).SeparatedBy(ls1).WithName("DirectiveDefine");
        ElseDirective = hashElse.Then(ls).Then(LetterOrDigit.Or(Punctuation).Not()).WithName("DirectiveElse");
        EndIfDirective = hashEndIf.Then(ls).Then(LetterOrDigit.Or(Punctuation).Not()).WithName("DirectiveEnd");
        IfDefDirective = (hashIfDef - (hashIfNDef | hashIf | hashEndIf)).Then(Identifier).SeparatedBy(ls1).WithName("DirectiveIfDef");
        IfNDefDirective = (hashIfNDef - (hashIf | hashEndIf)).Then(Identifier).SeparatedBy(ls1).WithName("DirectiveIfNDef");
        DefineDirective = hashDefine.Then(Identifier).Then(DirectiveExpr).SeparatedBy(ls1).WithName("DirectiveDefine");
        
    }
}
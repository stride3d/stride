using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;


namespace Stride.Shaders.Parsing.Grammars.Directive;

public partial class DirectiveGrammar : Grammar
{
    public DirectiveGrammar()
    {
        CreateAll();
    }

    public void CreateAll()
    {
        CreateTokens();
        CreateTokenGroups();
        CreateLiterals();
        CreateDirectives();
        CreateDirectiveExpressions();
    }
}

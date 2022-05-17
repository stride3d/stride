using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;


namespace Stride.Shader.Parsing
{
    public partial class DirectiveGrammar
    {
        public DirectiveGrammar()
        {
            CreateAll();
            Inner = Directives;
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
}
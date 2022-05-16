using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;


namespace Stride.Shader.Parsing
{
    public partial class DirectiveGrammar : SDSLGrammar
    {
        public DirectiveGrammar() : base()
        {
            Inner = Directives;
        }
    }
}
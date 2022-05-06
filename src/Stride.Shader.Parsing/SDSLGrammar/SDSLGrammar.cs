using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

using EtoParser = Eto.Parse.Parser;

namespace Stride.Shader.Parsing;
public partial class SDSLGrammar : Grammar
{
    public SDSLGrammar() : base("sdsl")
    {
        CreateAll();
        Inner = Shader;
    }

    public void CreateAll()
    {
        CreateTokens();
        CreateTokenGroups();
        CreateLiterals();
        CreateDirectives();
        CreateDirectiveExpressions();
        CreateExpressions();
        CreateStatements();
        CreateEntryPoints();
        CreateShader();
    }
}

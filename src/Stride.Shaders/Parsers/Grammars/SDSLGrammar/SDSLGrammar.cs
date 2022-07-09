using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace Stride.Shaders.Parsing.Grammars.SDSL;
public partial class SDSLGrammar : Grammar
{
    public SDSLGrammar() : base("sdsl")
    {
        CreateAll();
        Inner = ShaderFile;
    }

    public SDSLGrammar Using(Parser p)
    {
        Inner = p;
        return this;
    }

    public virtual void CreateAll()
    {
        CreateTokens();
        CreateTokenGroups();
        CreateLiterals();
        CreateDirectives();
        CreateDirectiveExpressions();
        CreateExpressions();
        CreateMethodDeclaration();
        CreateDeclarators();
        CreateConditionalFlowStatements();
        CreateLoopFlowStatements();
        CreateStatements();
        CreateShader();
    }
}

namespace Stride.Shaders.Parsing;

using Eto.Parse;
using Eto.Parse.Grammars;
using Stride.Shaders.Parsing.AST.Shader;
using Stride.Shaders.Parsing.Grammars;
using Stride.Shaders.Parsing.Grammars.Comments;
using Stride.Shaders.Parsing.Grammars.Directive;
using Stride.Shaders.Parsing.Grammars.Expression;
using Stride.Shaders.Parsing.Grammars.SDSL;
using System.Text;
public class ExpressionParser
{
    public ExpressionGrammar Grammar { get; set; } = new();

    public ShaderToken Parse(string expr)
    {
        var match = Grammar.Match(expr);
        if (!match.Success)
            throw new ArgumentOutOfRangeException("expr", string.Format("Invalid expr string: {0}", match.ErrorMessage));
        return ShaderToken.GetToken(match.Matches.First());
    }

}   
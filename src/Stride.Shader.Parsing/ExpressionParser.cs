namespace Stride.Shader.Parsing;

using Eto.Parse;
using Eto.Parse.Grammars;
using Stride.Shader.Parsing.AST.Shader;
using Stride.Shader.Parsing.Grammars;
using Stride.Shader.Parsing.Grammars.Comments;
using Stride.Shader.Parsing.Grammars.Directive;
using Stride.Shader.Parsing.Grammars.Expression;
using Stride.Shader.Parsing.Grammars.SDSL;
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
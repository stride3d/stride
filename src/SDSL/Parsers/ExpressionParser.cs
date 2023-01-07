namespace SDSL.Parsing;

using Eto.Parse;
using Eto.Parse.Grammars;
using SDSL.Parsing.AST.Shader;
using SDSL.Parsing.Grammars;
using SDSL.Parsing.Grammars.Comments;
using SDSL.Parsing.Grammars.Directive;
using SDSL.Parsing.Grammars.Expression;
using SDSL.Parsing.Grammars.SDSL;
using System.Text;
public class ExpressionParser
{
    public ExpressionGrammar Grammar { get; set; } = new();

    public ShaderToken Parse(string expr)
    {
        var match = Grammar.Match(expr);
        if (!match.Success)
            throw new ArgumentOutOfRangeException(nameof(expr), string.Format("Invalid expr string: {0}", match.ErrorMessage));
        return ShaderToken.Tokenize(match.Matches.First());
    }

}   
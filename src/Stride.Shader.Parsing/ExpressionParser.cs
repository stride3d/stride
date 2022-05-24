namespace Stride.Shader.Parsing;

using Eto.Parse;
using Eto.Parse.Grammars;
using Stride.Shader.Parsing.AST.Expressions;
using Stride.Shader.Parsing.Grammars;
using Stride.Shader.Parsing.Grammars.Comments;
using Stride.Shader.Parsing.Grammars.Directive;
using Stride.Shader.Parsing.Grammars.Expression;
using Stride.Shader.Parsing.Grammars.SDSL;
using System.Text;
public class ExpressionParser
{
    //public IEnumerable<string> Defined { get; set; }

    public ExpressionToken Parse(string expr)
    {
        return ExpressionToken.Parse(expr);
    }
}   
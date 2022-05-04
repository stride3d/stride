namespace Stride.Shader.Parsing;

using Eto.Parse;
using Eto.Parse.Grammars;
using System.Text;
public class SDSLParser
{
    public CommentGrammar Comments {get;set;}
    public SDSLGrammar SdslGrammar {get;set;}  

    public SDSLParser()
    {
        Comments = new();
        SdslGrammar = new();
    }

    public GrammarMatch Parse(string shader)
    {
        return Comments.Match(shader);
    }

}   
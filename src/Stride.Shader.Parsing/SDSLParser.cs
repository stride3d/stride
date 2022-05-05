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
        var comments = Comments.Match(shader);
        var actualCode = new StringBuilder();
        foreach(var m in comments.Matches)
        {
            if(m.Name == "ActualCode")
            {
                actualCode.Append(m.StringValue);
            }
        }
        var match = SdslGrammar.Match(actualCode.ToString());
        return match;
    }

}   
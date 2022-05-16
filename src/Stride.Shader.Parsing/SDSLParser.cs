namespace Stride.Shader.Parsing;

using Eto.Parse;
using Eto.Parse.Grammars;
using System.Text;
public class SDSLParser
{
    public CommentGrammar Comments {get;set;}
    public SDSLGrammar Grammar {get;set;}  
    public DirectiveGrammar Directive { get;set;}

    public SDSLParser()
    {
        Comments = new();
        Grammar = new();
    }

    public SDSLParser With(Parser p)
    {
        Grammar.Inner = p;
        return this;
    }

    public GrammarMatch Parse(string shader)
    {
        var comments = Comments.Match(shader);
        if(!comments.Matches.Any(x => x.Name == "Comment"))
        {
            return Grammar.Match(shader);
        }
        var actualCode = new StringBuilder();
        foreach(var m in comments.Matches)
        {
            if(m.Name == "ActualCode")
            {
                actualCode.Append(m.StringValue);
            }

        }
        var matches = Grammar.Match(actualCode.ToString());
        //if (matches.Errors.Any())
        //{
        //    throw new Exception("Parsing Exception : " + matches.ErrorMessage);
        //}
        return matches;
    }

}   
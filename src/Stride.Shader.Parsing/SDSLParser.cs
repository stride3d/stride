namespace Stride.Shader.Parsing;

using Eto.Parse;
using Eto.Parse.Grammars;
using Stride.Shader.Parsing.Grammars;
using Stride.Shader.Parsing.Grammars.Comments;
using Stride.Shader.Parsing.Grammars.Directive;
using Stride.Shader.Parsing.Grammars.SDSL;
using System.Text;
public class SDSLParser
{
    public CommentGrammar Comments {get;set;}
    public SDSLGrammar Grammar {get;set;}  
    public DirectiveGrammar Directive { get;set;}
    //public IEnumerable<string> Defined { get; set; }

    public SDSLParser()
    {
        Comments = new();
        Grammar = new();
        Directive = new();
    }

    public SDSLParser With(Parser p)
    {
        Grammar.Inner = p;
        return this;
    }

    public GrammarMatch Parse(string shader)
    {
        var comments = Comments.Match(shader);
        var preprocessed = new StringBuilder(); 
        if (!comments.Matches.Any(x => x.Name == "Comment"))
        {
            return Directive.Match(shader);
        }
        else
        {
            var actualCode = new StringBuilder();
            foreach (var m in comments.Matches)
            {
                if (m.Name == "ActualCode")
                {
                    actualCode.AppendLine(m.StringValue);
                }

            }
            //preprocessed.Add(this.PreProcessor())
            return PreProcessor(actualCode.ToString());
        }
    }

    public GrammarMatch PreProcessor(string code)
    {
        return Directive.Match(code);
    }

}   
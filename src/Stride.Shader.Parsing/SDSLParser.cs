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
    public StringBuilder UncommentedCode { get; set; } = new();
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

    public void PrintParserTree(string shader)
    {
        PrettyPrintMatches(Parse(shader).Matches[0]);
    }

    public GrammarMatch Parse(string shader)
    {
        var comments = Comments.Match(shader);
        //var preprocessed = new StringBuilder(); 
        if (!comments.Matches.Any(x => x.Name == "Comment"))
        {
            return Directive.Match(shader);
        }
        else
        {
            foreach (var m in comments.Matches)
            {
                if (m.Name == "ActualCode")
                {
                    UncommentedCode.AppendLine(m.StringValue);
                }
            }
            //preprocessed.Add(this.PreProcessor())
            return PreProcessor(UncommentedCode.ToString());
        }
    }

    public GrammarMatch PreProcessor(string code)
    {
        return Directive.Match(code);
    }

    private static void PrettyPrintMatches(Match match, int depth = 0)
    {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.Write(new string(' ', depth * 4) + match.Name);
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(" : " + match.StringValue);
        //Console.WriteLine(" : " + System.Text.RegularExpressions.Regex.Escape(match.StringValue)[..Math.Min(32,match.StringValue.Length)]);
        foreach (var m in match.Matches)
        {
            if (m.Matches.Count == 1 && m.Name.Contains("Expression"))
            {
                var tmp = m.Matches[0];
                while (tmp.Matches.Count == 1)
                {
                    tmp = tmp.Matches[0];
                }
                PrettyPrintMatches(tmp, depth + 1);
            }
            else
                PrettyPrintMatches(m, depth + 1);
        }
    }

}   
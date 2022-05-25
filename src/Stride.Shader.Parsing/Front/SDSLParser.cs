namespace Stride.Shader.Parsing;

using Eto.Parse;
using Eto.Parse.Grammars;
using Stride.Shader.Parsing.AST.Directives;
using Stride.Shader.Parsing.AST.Shader;
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
    public StringBuilder FinalCode { get; set; } = new();
    public Dictionary<string,object> Macros { get; set; } = new();

    public GrammarMatch? ParseTree { get; set; }
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
        PrettyPrintMatches(ParseTree.Matches[0]);
    }

    private void RemoveComments(string code)
    {
        var comments = Comments.Match(code);
        if (!comments.Matches.Any(x => x.Name == "Comment"))
        {
            UncommentedCode.Append(code);
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
        }
    }

    public GrammarMatch TestParse(string code)
    {
        return Grammar.Match(code);
    }

    public DirectiveToken ParseDirectives(string shader)
    {
        FinalCode.Clear();
        UncommentedCode.Clear();

        RemoveComments(shader);
        var matches = Directive.Match(UncommentedCode.ToString());
        if (!matches.Success)
            throw new Exception($"Parsing failed : \n{matches.ErrorMessage}");
        var tokens = DirectiveToken.GetToken(matches.Matches[0]);

        DirectiveToken.Evaluate(tokens, Macros, FinalCode);
        return tokens;
    }

    public ShaderToken Parse(string shader)
    {
        RemoveComments(shader);
        PreProcessor();
        return null;
    }

    public void PreProcessor()
    {
        DirectiveToken.GetToken(Directive.Match(UncommentedCode.ToString()));
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
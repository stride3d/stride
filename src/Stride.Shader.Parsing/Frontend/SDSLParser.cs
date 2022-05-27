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
using CppNet;


public class SDSLParser
{
    public SDSLGrammar Grammar {get;set;}
    public DirectivePreprocessor DPreprocessor { get; set; }
    public Preprocessor Preprocessor { get; set; }
    public Dictionary<string,object> Macros { get; set; } = new();

    public GrammarMatch? ParseTree { get; set; }

    public SDSLParser()
    {
        Grammar = new();
        DPreprocessor = new();


        Preprocessor = new();
        Preprocessor.addFeature(Feature.DIGRAPHS);
        Preprocessor.addWarning(Warning.IMPORT);
        Preprocessor.addFeature(Feature.INCLUDENEXT);
        //Preprocessor.addFeature(Feature.LINEMARKERS);
        Preprocessor.setListener(new ErrorListener());
    }

    public SDSLParser With(Parser p)
    {
        Grammar.Inner = p;
        return this;
    }

    public void PrintParserTree()
    {
        PrettyPrintMatches(ParseTree.Matches[0]);
    }

    public GrammarMatch TestParse(string code)
    {
        return Grammar.Match(code);
    }

    public void AddMacro(string name, object value)
    {
        Preprocessor.addMacro(name, value.ToString());
        DPreprocessor.Macros.Add(name, value);
    }
    public void AddMacro(string name)
    {
        Preprocessor.addMacro(name, string.Empty);
        DPreprocessor.Macros.Add(name, string.Empty);
    }

    public string DPreProcess(string code)
    {
        return DPreprocessor.PreProcess(code);
    }

    public string PreProcess(string code)
    {
        var inputSource = new StringLexerSource(code, true);
        Preprocessor.addInput(inputSource);
        var textBuilder = new StringBuilder();

        var isEndOfStream = false;
        while (!isEndOfStream)
        {
            Token tok = Preprocessor.token();
            switch (tok.getType())
            {
                case Token.EOF:
                    isEndOfStream = true;
                    break;
                case Token.CCOMMENT:
                    var strComment = tok.getText() ?? string.Empty;
                    foreach (var commentChar in strComment)
                    {
                        textBuilder.Append(commentChar == '\n' ? '\n' : ' ');
                    }
                    break;
                case Token.CPPCOMMENT:
                    break;
                default:
                    var tokenText = tok.getText();
                    if (tokenText != null)
                    {
                        textBuilder.Append(tokenText);
                    }
                    break;
            }
        }

        return textBuilder.ToString();
    }

    public ShaderToken Parse(string shader)
    {
        var code = PreProcess(shader);
        ParseTree = Grammar.Match(code);
        if (!ParseTree.Success)
            throw new Exception(ParseTree.ErrorMessage);
        return ShaderToken.GetToken(ParseTree);
        //return null;
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

    private class ErrorListener : DefaultPreprocessorListener
    {
        
    }
}   
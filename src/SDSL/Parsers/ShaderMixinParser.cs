namespace SDSL.Parsing;

using Eto.Parse;
using Eto.Parse.Grammars;
using SDSL.Parsing.AST.Directives;
using SDSL.Parsing.AST.Shader;
using SDSL.Parsing.Grammars;
using SDSL.Parsing.Grammars.Comments;
using SDSL.Parsing.Grammars.Directive;
using SDSL.Parsing.Grammars.SDSL;
using System.Text;
using CppNet;


public class ShaderMixinParser
{

    private static readonly ShaderMixinParser instance = new();
    public static ShaderProgram ParseShader(string shader) => instance.Parse(shader);
    public static List<string> GetMixins(string shader) => instance.ParseMixins(shader);

    

    public SDSLGrammar Grammar {get;set;}
    public DirectivePreprocessor DPreprocessor { get; set; }
    public Preprocessor Preprocessor { get; set; }

    public SDSLMixinReader MixinParser {get;set;}

    public GrammarMatch? ParseTree { get; set; }

    public ShaderMixinParser()
    {
        Grammar = new();
        MixinParser = new();
        DPreprocessor = new();


        Preprocessor = new();
        Preprocessor.addFeature(Feature.DIGRAPHS);
        Preprocessor.addWarning(Warning.IMPORT);
        Preprocessor.addFeature(Feature.INCLUDENEXT);
        //Preprocessor.addFeature(Feature.LINEMARKERS);
        // Preprocessor.setListener(new ErrorListener());
    }

    public ShaderMixinParser With(Parser p)
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

    public ShaderProgram Parse(string shader)
    {
        var code = PreProcess(shader);
        ParseTree = Grammar.Match(code);
        if (!ParseTree.Success)
            throw new Exception(ParseTree.ErrorMessage);
        return (ShaderProgram)ShaderToken.Tokenize(ParseTree);
        //return null;
    }
    List<string> ParseMixins(string shader)
    {
        var match = MixinParser.Match(shader);
        if (!match.Success)
            throw new Exception(match.ErrorMessage);
        return new();
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
using System.Text;
using CppNet;

namespace Stride.Shaders.Parsing;

public static class MonoGamePreProcessor
{    public static string Run(string filepath, ReadOnlySpan<(string Name, string Definition)> defines)
    {
        var file = File.ReadAllText(filepath);
        var filename = Path.GetFileName(filepath);
        var cpp = new Preprocessor();
        cpp.addFeature(Feature.DIGRAPHS);
        cpp.addWarning(Warning.IMPORT);
        cpp.addFeature(Feature.INCLUDENEXT);
        // cpp.addFeature(Feature.LINEMARKERS);

        // Pass defines
        if (defines != null)
        {
            foreach (var (Name, Definition) in defines)
            {
                if (!string.IsNullOrWhiteSpace(Name))
                {
                    cpp.addMacro(Name, Definition ?? string.Empty);
                }
            }
        }
        var inputSource = new StringLexerSource(file, true, filename);

        cpp.addInput(inputSource);

        var textBuilder = new StringBuilder();

        var isEndOfStream = false;
        while (!isEndOfStream)
        {
            Token tok = cpp.token();
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
}
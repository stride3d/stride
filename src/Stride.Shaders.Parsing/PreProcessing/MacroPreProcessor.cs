using System.Text;
using CppNet;

namespace Stride.Shaders.Parsing;

public static class MonoGamePreProcessor
{
    public static string OpenAndRun(string filepath, params ReadOnlySpan<(string Name, string Definition)> defines)
    {
        return Run(File.ReadAllText(filepath), Path.GetFileName(filepath), defines);
    }
    public static string Run(string content, string filename, params ReadOnlySpan<(string Name, string Definition)> defines)
    {
        var cpp = new Preprocessor();
        cpp.addFeature(Feature.DIGRAPHS);
        cpp.addWarning(Warning.IMPORT);
        cpp.addFeature(Feature.INCLUDENEXT);
        // cpp.addFeature(Feature.LINEMARKERS);

        // Pass defines
        if (!defines.IsEmpty)
        {
            foreach (var (Name, Definition) in defines)
            {
                if (!string.IsNullOrWhiteSpace(Name))
                {
                    cpp.addMacro(Name, Definition ?? string.Empty);
                }
            }
        }
        var inputSource = new StringLexerSource(content, true, filename);

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
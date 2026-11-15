using System.Text;
using CppNet;

namespace Stride.Shaders.Parsing;

public static class MonoGamePreProcessor
{
    public static string OpenAndRun(string filepath, params ReadOnlySpan<(string Name, string Definition)> defines)
    {
        return Run(File.ReadAllText(filepath), Path.GetFileName(filepath), defines);
    }

    public static string Run(string content, string? filename, params ReadOnlySpan<(string Name, string Definition)> defines)
        => Run(content, filename, null, defines);

    /// <summary>
    /// Runs the preprocessor on the code of a shader.
    /// </summary>
    /// <param name="onWarning">Receives each warning, in the form file(line,col): warning.</param>
    /// <exception cref="ShaderPreprocessorException">The code has a preprocessor error.</exception>
    public static string Run(string content, string? filename, Action<string>? onWarning, params ReadOnlySpan<(string Name, string Definition)> defines)
    {
        var cpp = new Preprocessor();
        cpp.setListener(new DiagnosticListener(filename, onWarning));
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
        // Byte-order marks at the start (an editor can save a second one) are not code: spaces keep the columns
        var byteOrderMarks = content.Length - content.TrimStart('﻿').Length;
        if (byteOrderMarks > 0)
            content = new string(' ', byteOrderMarks) + content[byteOrderMarks..];

        // A directive must end with a newline for CppNet, but editors can save a last line without one
        if (!content.EndsWith('\n'))
            content += '\n';

        // Note: CppNet is compiled in this project but doesn't support nullable
        var inputSource = new StringLexerSource(content, true, filename!);

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

    private sealed class DiagnosticListener(string? filename, Action<string>? onWarning) : PreprocessorListener
    {
        public void handleWarning(Source source, int line, int column, string msg) => onWarning?.Invoke(Format(line, column, msg));

        public void handleError(Source source, int line, int column, string msg) => throw new ShaderPreprocessorException(Format(line, column, msg));

        public void handleSourceChange(Source source, string ev) { }

        // CppNet counts columns from 0
        private string Format(int line, int column, string msg) => $"{MessageFileName.Of(filename)}({line},{column + 1}): {msg.TrimEnd()}";
    }
}

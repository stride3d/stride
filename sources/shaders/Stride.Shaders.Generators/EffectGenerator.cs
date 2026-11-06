using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Stride.Shaders.Parsing.SDFX;

[Generator]
public class EffectGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var shaderFiles =
            context
                .AdditionalTextsProvider
                .Where(x => Path.GetExtension(x.Path).ToLowerInvariant() is ".sdfx" or ".sdsl");

        // The C# types the struct keys can use: the candidate full names of every file, then the ones the compilation has. The
        // answer is a string, so a C# change regenerates the keys only when the answer changes.
        var candidates = shaderFiles.Select((file, _) => string.Join("\n", Generate(file, null).Candidates));
        var existingTypes = candidates.Collect().Combine(context.CompilationProvider).Select((x, _) => string.Join("\n", x.Left
            .SelectMany(names => names.Split('\n'))
            .Where(name => name.Length > 0 && x.Right.GetTypeByMetadataName(name) != null)
            .Distinct()
            .OrderBy(name => name, StringComparer.Ordinal)));

        context.RegisterSourceOutput(shaderFiles.Combine(existingTypes), (output, x) =>
        {
            var existing = new HashSet<string>(x.Right.Split('\n'));
            var generated = Generate(x.Left, existing.Contains);
            // The compiler doesn't show an #error of generated code when the project's own code has errors, which it has as soon as
            // this file generates nothing: the errors are diagnostics of the shader file instead
            foreach (var error in generated.Errors)
                output.ReportDiagnostic(ErrorDiagnostic(x.Left.Path, error));
            output.AddSource(GetSafeHintName(x.Left.Path), generated.Errors.Count > 0 ? "// The errors of this file are in the build output\n" : generated.Text);
        });
    }

    private static readonly ConcurrentDictionary<string, DiagnosticDescriptor> ErrorDescriptors = new();

    // The code of the message is the code of the diagnostic: "SDSL0251: ..."
    private static Diagnostic ErrorDiagnostic(string path, (string Message, int Line, int Column) error)
    {
        var message = error.Message;
        var id = "SDSL";
        if (message.Length > 10 && message.StartsWith("SDSL", StringComparison.Ordinal) && message.Substring(4, 4).All(char.IsDigit) && message.Substring(8, 2) == ": ")
        {
            id = message.Substring(0, 8);
            message = message.Substring(10);
        }
        var descriptor = ErrorDescriptors.GetOrAdd(id, x => new DiagnosticDescriptor(x, "Shader error", "{0}", "Stride.Shaders", DiagnosticSeverity.Error, isEnabledByDefault: true));
        var position = new LinePosition(Math.Max(error.Line - 1, 0), Math.Max(error.Column - 1, 0));
        var location = error.Line > 0 ? Location.Create(path, default, new LinePositionSpan(position, position)) : Location.None;
        return Diagnostic.Create(descriptor, location, message);
    }

    /// <summary>
    /// Generates the keys and effects of a shader file.
    /// </summary>
    /// <param name="csharpTypeExists">Whether a C# type exists, by full name, for the keys of struct type (see <see cref="EffectCodeWriter.CSharpTypeExists"/>).</param>
    /// <returns>The code, the full names the C# types of its struct keys can have, and its errors with their line and column (0 when unknown).</returns>
    public static (string Text, IEnumerable<string> Candidates, IReadOnlyList<(string Message, int Line, int Column)> Errors) Generate(AdditionalText file, Func<string, bool>? csharpTypeExists)
    {
        var isSdfx = Path.GetExtension(file.Path).Equals(".sdfx", StringComparison.OrdinalIgnoreCase);

        try
        {
            var preprocessedText = MonoGamePreProcessor.Run(file.GetText()?.ToString() ?? throw new InvalidOperationException($"Could not read file content for {file.Path}"), file.Path);
            var parsed = SDSLParser.Parse(preprocessedText);
            if (parsed.Errors.Count > 0)
            {
                var sb = new StringBuilder();
                foreach (var error in parsed.Errors)
                    sb.AppendLine($"#error Parse error: {error}");
                return (sb.ToString(), [], [.. parsed.Errors.Select(x => (x.Message, x.Location.Line, x.Location.Column))]);
            }
            else if (parsed.AST == null)
            {
                return ("#error No AST parsed", [], [("No AST parsed", 0, 0)]);
            }

            // .sdfx files must declare 'effect', not 'shader' — catch the mistake here
            // instead of silently emitting unbuildable C#.
            if (isSdfx && parsed.AST is Parsing.ShaderFile sdfxFile && HasShaderClassDeclaration(sdfxFile))
            {
                var message = $"'{Path.GetFileName(file.Path)}' contains a 'shader' declaration. Use 'effect' (or 'partial effect') inside .sdfx files; 'shader' / 'partial shader' belongs in .sdsl.";
                return ($"#error {message}", [], [(message, 0, 0)]);
            }

            var effectCodeWriter = new EffectCodeWriter { CSharpTypeExists = csharpTypeExists };
            effectCodeWriter.Run(parsed.AST);
            return (effectCodeWriter.Text, effectCodeWriter.CSharpTypeCandidates, [.. effectCodeWriter.Errors.Select(x => (x.Message, x.Location.Line, x.Location.Column))]);
        }
        catch (ShaderPreprocessorException e)
        {
            return ($"#error {e.Message}", [], [(e.Message, 0, 0)]);
        }
        catch (Exception e)
        {
            return ($"#error Exception while running {nameof(EffectGenerator)} {e}", [], [($"Exception while running {nameof(EffectGenerator)} {e}", 0, 0)]);
        }
    }

    private static bool HasShaderClassDeclaration(Parsing.ShaderFile ast)
    {
        foreach (var d in ast.RootDeclarations)
            if (d is Parsing.SDSL.AST.ShaderClass)
                return true;
        foreach (var ns in ast.Namespaces)
            foreach (var d in ns.Declarations)
                if (d is Parsing.SDSL.AST.ShaderClass)
                    return true;
        return false;
    }

    public static string GetSafeHintName(string absolutePath)
    {
        // 1. Get the file name without extension (e.g., "MyConfig")
        string fileName = Path.GetFileName(absolutePath);

        // 2. Sanitize: Replace characters that are invalid in file names
        char[] invalidChars = Path.GetInvalidFileNameChars();
        string sanitizedName = new string(fileName
            .Select(c => invalidChars.Contains(c) ? '_' : c)
            .ToArray());

        // 3. Create a deterministic hash of the full path to avoid collisions
        // for files with the same name in different directories.
        using var sha1 = SHA1.Create();
        byte[] hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(absolutePath));
        string hash = BitConverter.ToString(hashBytes).Replace("-", "").Substring(0, 8);

        // 4. Combine into a stable hint name (e.g., "MyConfig_A1B2C3D4.g.cs")
        return $"{sanitizedName}_{hash}.g.cs";
    }
}

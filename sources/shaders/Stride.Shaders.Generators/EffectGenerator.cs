using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
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
            output.AddSource(GetSafeHintName(x.Left.Path), Generate(x.Left, existing.Contains).Text);
        });
    }

    /// <summary>
    /// Generates the keys and effects of a shader file.
    /// </summary>
    /// <param name="csharpTypeExists">Whether a C# type exists, by full name, for the keys of struct type (see <see cref="EffectCodeWriter.CSharpTypeExists"/>).</param>
    /// <returns>The code, and the full names the C# types of its struct keys can have.</returns>
    public static (string Text, IEnumerable<string> Candidates) Generate(AdditionalText file, Func<string, bool>? csharpTypeExists)
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
                return (sb.ToString(), []);
            }
            else if (parsed.AST == null)
            {
                return ("#error No AST parsed", []);
            }

            // .sdfx files must declare 'effect', not 'shader' — catch the mistake here
            // instead of silently emitting unbuildable C#.
            if (isSdfx && parsed.AST is Parsing.ShaderFile sdfxFile && HasShaderClassDeclaration(sdfxFile))
                return ($"#error '{Path.GetFileName(file.Path)}' contains a 'shader' declaration. Use 'effect' (or 'partial effect') inside .sdfx files; 'shader' / 'partial shader' belongs in .sdsl.", []);

            var effectCodeWriter = new EffectCodeWriter { CSharpTypeExists = csharpTypeExists };
            effectCodeWriter.Run(parsed.AST);
            return (effectCodeWriter.Text, effectCodeWriter.CSharpTypeCandidates);
        }
        catch (Exception e)
        {
            return ($"#error Exception while running {nameof(EffectGenerator)} {e}", []);
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

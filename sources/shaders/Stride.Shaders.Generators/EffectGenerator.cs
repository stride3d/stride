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

        context.RegisterSourceOutput(shaderFiles, GenerateShaderKeysAndEffects);
    }

    private void GenerateShaderKeysAndEffects(SourceProductionContext arg1, AdditionalText arg2)
    {
        var filename = GetSafeHintName(arg2.Path);
        var isSdfx = Path.GetExtension(arg2.Path).Equals(".sdfx", StringComparison.OrdinalIgnoreCase);

        try
        {
            var preprocessedText = MonoGamePreProcessor.Run(arg2.GetText()?.ToString() ?? throw new InvalidOperationException($"Could not read file content for {arg2.Path}"), arg2.Path);
            var parsed = SDSLParser.Parse(preprocessedText);
            if (parsed.Errors.Count > 0)
            {
                var sb = new StringBuilder();
                foreach (var error in parsed.Errors)
                    sb.AppendLine($"#error Parse error: {error}");
                arg1.AddSource(filename, sb.ToString());
                return;
            }
            else if (parsed.AST == null)
            {
                arg1.AddSource(filename, "#error No AST parsed");
                return;
            }

            // .sdfx files must declare 'effect', not 'shader' — catch the mistake here
            // instead of silently emitting unbuildable C#.
            if (isSdfx && parsed.AST is Parsing.ShaderFile sdfxFile && HasShaderClassDeclaration(sdfxFile))
            {
                arg1.AddSource(filename, $"#error '{Path.GetFileName(arg2.Path)}' contains a 'shader' declaration. Use 'effect' (or 'partial effect') inside .sdfx files; 'shader' / 'partial shader' belongs in .sdsl.");
                return;
            }

            var effectCodeWriter = new EffectCodeWriter();
            effectCodeWriter.Run(parsed.AST);
            arg1.AddSource(filename, effectCodeWriter.Text);
        }
        catch (Exception e)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"#error Exception while running {nameof(EffectGenerator)} {e}");
            arg1.AddSource(filename, sb.ToString());
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

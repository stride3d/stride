using Microsoft.CodeAnalysis;
using Stride.Shaders.Generators.Intrinsics;

namespace Stride.Shaders.Generators;

[Generator]
internal class IntrinsicsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var file = 
            context
            .AdditionalTextsProvider
            .Where(x => x.Path.EndsWith("gen_intrin_main.txt"))
            .SelectMany(ParseInstrinsics);
    }


    static List<IntrinsicDeclaration> ParseInstrinsics(AdditionalText text, CancellationToken ct)
    {
        var scanner = new Scanner(text.GetText(ct)?.ToString() ?? "");

        return null!;
    }
}
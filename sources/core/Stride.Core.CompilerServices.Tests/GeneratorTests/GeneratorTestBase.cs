using System.Text;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Stride.Core.CompilerServices.Generators;
using Stride.Core.Reflection;

namespace Stride.Core.CompilerServices.Tests.GeneratorTests;

public abstract class GeneratorTestBase
{
    protected CSharpSourceGeneratorTest<UnifiedGenerator, DefaultVerifier> Context { get; }

    public GeneratorTestBase()
    {
        Context = new()
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net100
        };
        Context.TestState.AdditionalReferences.Add(typeof(AssemblyScanAttribute).Assembly);
    }

    protected async Task TestGeneration(string testCode, string expectedGeneratedCode, string expectedFileName = UnifiedGenerator.FileName, CancellationToken cancellationToken = default)
    {
        Context.TestCode = testCode;
        Context.TestState.GeneratedSources.Add((typeof(UnifiedGenerator), expectedFileName, SourceText.From(expectedGeneratedCode.Replace("\r", null), Encoding.UTF8, SourceHashAlgorithm.Sha256)));
        await Context.RunAsync(cancellationToken);
    }
}

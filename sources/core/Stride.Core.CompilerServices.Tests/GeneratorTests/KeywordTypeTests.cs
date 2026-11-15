using Xunit;

namespace Stride.Core.CompilerServices.Tests.GeneratorTests;

public class KeywordTypeTests : UnifiedGeneratorTestBase
{
    [Fact]
    public async Task EscapesKeywordTypeNamesInGeneratedCode()
    {
        string testCode = """
            [global::Stride.Core.Reflection.AssemblyScan]
            public class @class {}
            public class Candidate : @class {}
            """;

        await TestGeneration(testCode, ExpectedAssemblyScanCode("@class", ["@class", "Candidate"]));
    }
}

using Xunit;

namespace Stride.Core.CompilerServices.Tests.GeneratorTests;

public class NestedTypeTests : UnifiedGeneratorTestBase
{
    [Fact]
    public async Task ScansPublicNestedTypesButExcludesPrivateNestedTypes()
    {
        string testCode = """
            public class Outer
            {
                [global::Stride.Core.Reflection.AssemblyScan]
                public class PublicTarget {}
                public class PublicCandidate : PublicTarget {}
                [global::Stride.Core.Reflection.AssemblyScan]
                private class PrivateTarget {}
                private class PrivateCandidate : PublicTarget {}
            }
            """;

        await TestGeneration(testCode, ExpectedAssemblyScanCode("Outer.PublicTarget", ["Outer.PublicTarget", "Outer.PublicCandidate"]));
    }
}

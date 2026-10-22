using Xunit;

namespace Stride.Core.CompilerServices.Tests.GeneratorTests;

public class DerivedClassTests : UnifiedGeneratorTestBase
{
    [Fact]
    public async Task ScanningForBaseClass()
    {
        string testCode = """
            [Stride.Core.Reflection.AssemblyScan]
            public class BaseClass {}
            public class DerivedClass : BaseClass {}
            """;
        await TestGeneration(testCode, ExpectedAssemblyScanCode("BaseClass", ["BaseClass", "DerivedClass"]));
    }
}

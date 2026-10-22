using Xunit;

namespace Stride.Core.CompilerServices.Tests.GeneratorTests;

public class GenericInterfaceTests : UnifiedGeneratorTestBase
{
    [Fact]
    public async Task ScanningForGenericInterface()
    {
        string testCode = """
            [global::Stride.Core.Reflection.AssemblyScan]
            public interface IMarker<T> {}
            public class Candidate : IMarker<int> {}
            """;

        await TestGeneration(testCode, ExpectedAssemblyScanCode("IMarker<>", ["Candidate"]));
    }
}

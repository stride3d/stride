using Xunit;

namespace Stride.Core.CompilerServices.Tests.GeneratorTests;

public class InterfaceTests : UnifiedGeneratorTestBase
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

    [Fact]
    public async Task ScanningForInterfaceFromReference()
    {
        string testCode = """
        public class Provider : global::Stride.Core.Settings.IAppSettingsProvider
        {
            public global::Stride.Core.Settings.AppSettings LoadAppSettings() => new();
        }
        """;

        await TestGeneration(testCode, ExpectedAssemblyScanCode("Stride.Core.Settings.IAppSettingsProvider", ["Provider"]));
    }
}

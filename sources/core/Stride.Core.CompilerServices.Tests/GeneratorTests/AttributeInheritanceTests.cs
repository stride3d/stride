using Xunit;

namespace Stride.Core.CompilerServices.Tests.GeneratorTests;

public class AttributeInheritanceTests : UnifiedGeneratorTestBase
{
    [Fact]
    public async Task ScanningIncludesInheritedAttributesWhenEnabled()
    {
        string testCode = """
            [global::System.AttributeUsage(global::System.AttributeTargets.Class, Inherited = true)]
            [global::Stride.Core.Reflection.AssemblyScan]
            public sealed class ScanAttribute : global::System.Attribute;
            [Scan]
            public class BaseClass {}
            public class DerivedClass : BaseClass {}
            """;

        await TestGeneration(testCode, ExpectedAssemblyScanCode("ScanAttribute", ["BaseClass", "DerivedClass"]));
    }

    [Fact]
    public async Task ScanningExcludesInheritedAttributesWhenDisabled()
    {
        string testCode = """
            [global::System.AttributeUsage(global::System.AttributeTargets.Class, Inherited = false)]
            [global::Stride.Core.Reflection.AssemblyScan]
            public sealed class ScanAttribute : global::System.Attribute;
            [Scan]
            public class BaseClass {}
            public class DerivedClass : BaseClass {}
            """;

        await TestGeneration(testCode, ExpectedAssemblyScanCode("ScanAttribute", ["BaseClass"]));
    }
}

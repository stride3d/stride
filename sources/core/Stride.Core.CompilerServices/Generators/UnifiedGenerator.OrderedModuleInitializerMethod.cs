namespace Stride.Core.CompilerServices.Generators;

internal partial class UnifiedGenerator
{
    private readonly record struct OrderedModuleInitializerMethod(int Order, string InvocationString);
}

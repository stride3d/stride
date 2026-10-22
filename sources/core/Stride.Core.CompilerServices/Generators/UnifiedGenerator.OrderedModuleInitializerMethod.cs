namespace Stride.Core.CompilerServices.Generators;

public partial class UnifiedGenerator
{
    private readonly record struct OrderedModuleInitializerMethod(int Order, string InvocationString);
}

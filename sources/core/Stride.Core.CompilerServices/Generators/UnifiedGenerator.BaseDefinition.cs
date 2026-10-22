namespace Stride.Core.CompilerServices.Generators;

internal partial class UnifiedGenerator
{
    private abstract record BaseDefinition
    {
        public abstract string FullName { get; }
    }
}

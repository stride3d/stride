namespace Stride.Core.CompilerServices.Common;

internal static class IncrementalValuesProviderExtensions
{
    public static IncrementalValuesProvider<T> WhereNotNull<T>(this IncrementalValuesProvider<T?> provider) where T : class
    {
        return provider.Where(static (item) => item is not null)!;
    }
}

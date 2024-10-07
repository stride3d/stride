using Stride.Shaders.Spirv.PostProcessing;

namespace Stride.Shaders.Spirv;

/// <summary>
/// Repository for compositable shaders
/// </summary>
public class CompositionSourceProvider
{
    internal static CompositionSourceProvider Instance { get; } = new();

    readonly Dictionary<string, Composable> Composables;

    private CompositionSourceProvider()
    {
        Composables = new();
    }

    public static void CompileAndRegister(string name)
    {
        var buffer = PostProcessor.Process(name).ToSorted();
        Register(new(name,buffer));
    }

    public static void Register(Composable composable)
    {
        Instance.Composables.Add(composable.Name, composable);
    }
    public static Composable Get(string name)
    {
        return Instance.Composables[name];
    }
}
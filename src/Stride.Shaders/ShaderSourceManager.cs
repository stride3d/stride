using System.Diagnostics.CodeAnalysis;

namespace Stride.Shaders;

public class ShaderSourceManager
{

    private readonly object locker = new object();
    private readonly Dictionary<string, ShaderSourceWithHash> loadedShaderSources = new();
    private readonly Dictionary<string, string> classNameToPath = new();
    private HashSet<ShaderSourceWithHash> shaders = new();

    private List<string> lookups = new();

    private const string DefaultEffectFileExtension = ".sdsl";

    public ShaderSourceManager() { }


    public void AddDirectory(string path)
    {
        lookups.Add(path);
        foreach(var p in lookups)
        {
            Directory.EnumerateFiles(p,"*.sdsl",SearchOption.AllDirectories)
            .Select(x => new ShaderSourceWithHash{ Path = x, Source = File.ReadAllText(x)})
            .ToList().ForEach(_ => shaders.Add(_));
        }
    }


    public void AddShaderSource(string className, string source, string path)
    {
        var shaderSource = new ShaderSourceWithHash { Path = path, Source = source };
        loadedShaderSources[className] = shaderSource;
        classNameToPath[className] = path;
    }

    public string GetShaderSource(string className)
    {
        return loadedShaderSources[className].Source;
    }


    public static ShaderSourceWithHash CreateShaderSourceWithHash(string type, string source)
    {
        return new ShaderSourceWithHash
        {
            Path = type,
            Source = source
        };
    }

    public struct ShaderSourceWithHash
    {
        public string Source;
        public string Path;
        public int Hash => GetHashCode();

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is ShaderSourceWithHash other
                && other.Hash == this.Hash;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(Source);
        }
    }
}
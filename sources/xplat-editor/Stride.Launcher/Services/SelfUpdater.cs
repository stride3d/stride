using System.Reflection;

namespace Stride.Launcher.Services;

public static class SelfUpdater
{
    public static readonly string? Version;

    static SelfUpdater()
    {
        var assembly = Assembly.GetEntryAssembly();
        var assemblyInformationalVersion = assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        Version = assemblyInformationalVersion?.InformationalVersion;
    }
}

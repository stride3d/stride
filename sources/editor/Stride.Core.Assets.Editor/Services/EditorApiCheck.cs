// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Stride.Core.Diagnostics;

#nullable enable

namespace Stride.Core.Assets.Editor.Services;

/// <summary>
/// The editor's check of a plugin assembly before it loads: it reads the versions of the editor assemblies the plugin was
/// built against (<see cref="ContractAssemblies"/>) without loading it, and refuses a plugin built for a newer editor.
/// </summary>
/// <remarks>
/// <para>
/// The version that counts is the assembly version, which is <c>major.minor.0.0</c> for every Stride assembly: a patch
/// release keeps it, so a patch difference never counts. A plugin built against a newer major or minor version than the
/// editor is refused. One built against the same or an older version loads; if it then uses a member that is gone, the
/// contribution guard turns that contribution off (<see cref="IPluginContributionGuard"/>).
/// </para>
/// <para>
/// An assembly that references none of the editor assemblies (a runtime or an Assets assembly) loads as before, and so does
/// one the check cannot read: the loader reports its own errors.
/// </para>
/// </remarks>
[Experimental("STREXP002")]
public sealed class EditorApiCheck : IAssemblyLoadCheck
{
    private readonly Action<IncompatiblePluginException>? refused;

    /// <summary>
    /// Creates a check against the version of the running editor.
    /// </summary>
    /// <param name="refused">Called for each assembly the check refuses, after it is logged.</param>
    public EditorApiCheck(Action<IncompatiblePluginException>? refused = null)
        : this(typeof(EditorApiCheck).Assembly.GetName().Version ?? new Version(0, 0), refused)
    {
    }

    /// <summary>
    /// Creates a check against a given editor version.
    /// </summary>
    /// <param name="editorVersion">The version of the editor (only its major and minor parts count).</param>
    /// <param name="refused">Called for each assembly the check refuses, after it is logged.</param>
    public EditorApiCheck(Version editorVersion, Action<IncompatiblePluginException>? refused = null)
    {
        ArgumentNullException.ThrowIfNull(editorVersion);
        EditorVersion = new Version(editorVersion.Major, editorVersion.Minor);
        this.refused = refused;
    }

    /// <summary>
    /// The editor assemblies whose versions the check compares: the toolkit-neutral ones and those of each toolkit, which
    /// plugins reference to add to the editor.
    /// </summary>
    public static IReadOnlySet<string> ContractAssemblies { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        // Toolkit-neutral
        "Stride.Core.Presentation",
        "Stride.Core.Presentation.Quantum",
        "Stride.Core.Assets.Presentation",
        "Stride.Core.Assets.Editor",
        "Stride.Assets.Presentation",
        "Stride.Editor",
        "Stride.Assets.Editor",
        // Avalonia
        "Stride.Core.Presentation.Avalonia",
        "Stride.Core.Assets.Editor.Avalonia",
        "Stride.Editor.Avalonia",
        "Stride.Assets.Editor.Avalonia",
        // WPF
        "Stride.Core.Presentation.Wpf",
        "Stride.Core.Assets.Editor.Wpf",
        "Stride.Editor.Wpf",
        "Stride.Assets.Editor.Wpf",
    };

    /// <summary>
    /// The version of the editor, major and minor.
    /// </summary>
    public Version EditorVersion { get; }

    /// <summary>
    /// Reads which editor assembly an assembly references at the highest version, without loading it.
    /// </summary>
    /// <param name="assemblyPath">The path of the assembly.</param>
    /// <param name="contractAssembly">The editor assembly referenced at that version, or null.</param>
    /// <returns>The highest version of an editor assembly that the assembly references, or null when it references none
    /// (a native library references none either).</returns>
    /// <exception cref="IOException">The file cannot be read.</exception>
    /// <exception cref="BadImageFormatException">The file is not a valid assembly.</exception>
    public static Version? GetReferencedEditorVersion(string assemblyPath, out string? contractAssembly)
    {
        ArgumentNullException.ThrowIfNull(assemblyPath);
        contractAssembly = null;
        using var stream = File.OpenRead(assemblyPath);
        using var reader = new PEReader(stream);
        if (!reader.HasMetadata)
            return null;

        var metadata = reader.GetMetadataReader();
        Version? highest = null;
        foreach (var handle in metadata.AssemblyReferences)
        {
            var reference = metadata.GetAssemblyReference(handle);
            var name = metadata.GetString(reference.Name);
            if (!ContractAssemblies.Contains(name))
                continue;
            if (highest is null || reference.Version > highest)
            {
                highest = reference.Version;
                contractAssembly = name;
            }
        }
        return highest;
    }

    /// <summary>
    /// Checks an assembly without loading it.
    /// </summary>
    /// <param name="assemblyPath">The path of the assembly.</param>
    /// <param name="packageName">The name of the package that brings it, for the error, or null.</param>
    /// <returns>Null when the assembly can load; otherwise the error that says which editor it needs.</returns>
    /// <exception cref="IOException">The file cannot be read.</exception>
    /// <exception cref="BadImageFormatException">The file is not a valid assembly.</exception>
    public IncompatiblePluginException? Check(string assemblyPath, string? packageName = null)
    {
        var referenced = GetReferencedEditorVersion(assemblyPath, out var contractAssembly);
        if (referenced is null || contractAssembly is null)
            return null;

        // Only major.minor counts: a patch release keeps the assembly version
        var required = new Version(referenced.Major, referenced.Minor);
        if (required <= EditorVersion)
            return null;
        return new IncompatiblePluginException(assemblyPath, packageName, contractAssembly, referenced, required, EditorVersion);
    }

    /// <inheritdoc />
    public bool CanLoad(Package package, string assemblyPath, ILogger log)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(log);

        IncompatiblePluginException? error;
        try
        {
            error = Check(assemblyPath, package.Meta.Name);
        }
        catch (Exception e) when (e is IOException or BadImageFormatException or UnauthorizedAccessException)
        {
            log.Warning($"The editor could not read the references of [{assemblyPath}] to check which Game Studio it was built for; it loads it as usual.", e);
            return true;
        }
        if (error is null)
            return true;

        log.Warning(error.Message);
        refused?.Invoke(error);
        return false;
    }
}

/// <summary>
/// The error of a plugin built for a newer editor than the one running: it is not loaded (<see cref="PluginState.Incompatible"/>).
/// </summary>
[Experimental("STREXP002")]
public sealed class IncompatiblePluginException : Exception
{
    /// <param name="assemblyPath">The path of the plugin's assembly.</param>
    /// <param name="packageName">The name of the package that brings it, or null.</param>
    /// <param name="contractAssembly">The editor assembly it references at the highest version.</param>
    /// <param name="referencedVersion">The version of that reference.</param>
    /// <param name="requiredEditorVersion">The editor version it needs, major and minor.</param>
    /// <param name="editorVersion">The version of the running editor, major and minor.</param>
    public IncompatiblePluginException(string assemblyPath, string? packageName, string contractAssembly, Version referencedVersion, Version requiredEditorVersion, Version editorVersion)
        : base(FormatMessage(assemblyPath, packageName, contractAssembly, referencedVersion, requiredEditorVersion, editorVersion))
    {
        AssemblyPath = assemblyPath;
        AssemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
        PackageName = packageName;
        ContractAssembly = contractAssembly;
        ReferencedVersion = referencedVersion;
        RequiredEditorVersion = requiredEditorVersion;
        EditorVersion = editorVersion;
    }

    /// <summary>
    /// The path of the plugin's assembly.
    /// </summary>
    public string AssemblyPath { get; }

    /// <summary>
    /// The name of the plugin's assembly.
    /// </summary>
    public string AssemblyName { get; }

    /// <summary>
    /// The name of the package that brings it, or null.
    /// </summary>
    public string? PackageName { get; }

    /// <summary>
    /// The editor assembly it references at the highest version.
    /// </summary>
    public string ContractAssembly { get; }

    /// <summary>
    /// The version of that reference.
    /// </summary>
    public Version ReferencedVersion { get; }

    /// <summary>
    /// The editor version the plugin needs, major and minor.
    /// </summary>
    public Version RequiredEditorVersion { get; }

    /// <summary>
    /// The version of the running editor, major and minor.
    /// </summary>
    public Version EditorVersion { get; }

    private static string FormatMessage(string assemblyPath, string? packageName, string contractAssembly, Version referencedVersion, Version requiredEditorVersion, Version editorVersion)
    {
        var name = Path.GetFileNameWithoutExtension(assemblyPath);
        var package = packageName is not null && !string.Equals(packageName, name, StringComparison.OrdinalIgnoreCase) ? $" of the package [{packageName}]" : "";
        return $"The plugin [{name}]{package} needs Game Studio {requiredEditorVersion.ToString(2)}: it was built against {contractAssembly} {referencedVersion}, and this Game Studio is {editorVersion.ToString(2)}. It is not loaded.";
    }
}

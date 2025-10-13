// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.CodeEditorSupport;

public sealed class IDEInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IDEInfo"/> class.
    /// </summary>
    /// <param name="displayName">The display name of the IDE instance</param>
    /// <param name="programPath">The path to the installation root of the IDE instance.</param>
    /// <param name="ideType">The type of IDE instance</param>
    /// <param name="installationVersion">The version of the IDE instance.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public IDEInfo(string displayName, string? programPath, IDEType ideType, Version? installationVersion = null)
    {
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        ProgramPath = programPath;
        IDEType = ideType;
        InstallationVersion = installationVersion;
    }
    
    public static readonly IDEInfo DefaultIDE = new( "Default IDE", string.Empty, IDEType.VisualStudio);

    /// <summary> 
    /// Gets the type of the IDE. 
    /// </summary>
    public IDEType IDEType { get; }

    /// <summary> 
    /// Gets the display name (title) of the product installed in this instance. 
    /// </summary>
    public string DisplayName { get; }

    /// <summary>Gets the version of the product installed in this instance.</summary>
    /// <value>The version of the product installed in this instance.</value>
    public Version? InstallationVersion { get; }

    /// <summary>
    /// The path to the executable of this IDE, or <c>null</c>.
    /// </summary>
    public string? ProgramPath { get; }

    /// <summary>
    /// The path to the VSIX installer of this IDE, or <c>null</c>.
    /// </summary>
    public string? VsixInstallerPath { get; init; }

    /// <summary>
    /// The package names and versions of packages installed to this instance.
    /// </summary>
    /// <value></value>
    public Dictionary<string, string> PackageVersions { get; } = [];

    /// <summary>
    /// <c>true</c> if this IDE has a development environment; otherwise, <c>false</c>.
    /// </summary>
    public bool HasProgram => !string.IsNullOrEmpty(ProgramPath);

    /// <summary>
    /// <c>true</c> if this IDE has a VSIX installer; otherwise, <c>false</c>.
    /// </summary>
    public bool HasVsixInstaller => !string.IsNullOrEmpty(VsixInstallerPath);

    /// <inheritdoc />
    public override string ToString() => DisplayName;
}

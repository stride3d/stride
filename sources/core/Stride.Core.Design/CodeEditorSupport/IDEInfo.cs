// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.CodeEditorSupport;

public sealed class IDEInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IDEInfo"/> class.
    /// </summary>
    /// <param name="installationVersion">The version of the IDE instance.</param>
    /// <param name="displayName">The display name of the IDE instance</param>
    /// <param name="programPath">The path to the installation root of the IDE instance.</param>
    /// <param name="instanceId">The unique identifier for this installation instance.</param>
    /// <param name="ideType">The type of IDE instance</param>
    /// <exception cref="ArgumentNullException"></exception>
    public IDEInfo(Version installationVersion, string displayName, string? programPath, string instanceId, IDEType ideType)
    {
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        InstallationVersion = installationVersion ?? throw new ArgumentNullException(nameof(installationVersion));
        InstanceId = instanceId ?? throw new ArgumentNullException(nameof(instanceId));
        ProgramPath = programPath;
        IDEType = ideType;
    }
    
    public static readonly IDEInfo DefaultIDE = new(new Version("0.0"), "Default IDE", string.Empty, string.Empty, IDEType.VisualStudio);

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
    public Version InstallationVersion { get; }

    /// <summary>
    /// The path to the executable of this IDE, or <c>null</c>.
    /// </summary>
    public string? ProgramPath { get; }
    
    /// <summary>
    /// The hex code for this installation instance. It is used, for example, to create a unique folder in %LocalAppData%
    /// </summary>
    public string InstanceId { get; }

    /// <summary>
    /// The path to the VSIX installer of this IDE, or <c>null</c>.
    /// </summary>
    public string? VsixInstallerPath { get; set; }

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

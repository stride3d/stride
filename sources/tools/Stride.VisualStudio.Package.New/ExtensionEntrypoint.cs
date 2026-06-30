// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Extensibility;

namespace Stride.VisualStudio;

/// <summary>
/// Entry point of the Stride out-of-process Visual Studio extension.
/// </summary>
[VisualStudioContribution]
internal sealed class ExtensionEntrypoint : Extension
{
    public override ExtensionConfiguration ExtensionConfiguration => new()
    {
        Metadata = new(
            id: "Stride.VisualStudio.Package",
            version: this.ExtensionAssemblyVersion,
            publisherName: "Stride",
            displayName: "Stride",
            description: "Visual Studio integration for the Stride game engine.")
        {
            Icon = "Resources/GameStudio.png",
        },
    };
}

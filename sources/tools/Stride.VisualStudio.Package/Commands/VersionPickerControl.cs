// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Extensibility.UI;

namespace Stride.VisualStudio.Commands;

/// <summary>
/// Remote WPF control that lets the user pick a Stride version from a drop-down.
/// </summary>
internal sealed class VersionPickerControl : RemoteUserControl
{
    public VersionPickerControl(VersionPickerData dataContext)
        : base(dataContext)
    {
    }
}

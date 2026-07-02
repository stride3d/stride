// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.VisualStudio.Extensibility.UI;

namespace Stride.VisualStudio.Commands;

/// <summary>
/// Remote WPF control that shows shader-regeneration progress: a live log with a progress bar, then a result.
/// </summary>
internal sealed class ShaderRegenControl : RemoteUserControl
{
    public ShaderRegenControl(ShaderRegenData dataContext)
        : base(dataContext)
    {
    }
}

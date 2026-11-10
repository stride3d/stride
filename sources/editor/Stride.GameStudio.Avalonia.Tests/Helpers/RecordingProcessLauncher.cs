// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.GameStudio.Avalonia.Services;

namespace Stride.GameStudio.Avalonia.Tests.Helpers;

/// <summary>
/// A launcher of new editor processes that records their command lines instead of starting them.
/// </summary>
internal sealed class RecordingProcessLauncher : IEditorProcessLauncher
{
    public List<IReadOnlyList<string>> Starts { get; } = [];

    public EditorProcessService Service { get; set; } = null!;

    public Action? Started { get; set; }

    public void Start(IReadOnlyList<string> arguments)
    {
        Starts.Add([.. arguments]);
        Started?.Invoke();
    }
}

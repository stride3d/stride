// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.GameStudio.Mcp;

/// <summary>
/// Thrown when dialog(s) were suppressed during MCP tool execution.
/// Carries the dialog messages so they can be reported back to the agent as errors.
/// </summary>
public sealed class McpDialogSuppressedException : Exception
{
    public IReadOnlyList<string> DialogMessages { get; }

    public McpDialogSuppressedException(IReadOnlyList<string> dialogMessages)
        : base($"Editor showed {dialogMessages.Count} dialog(s) during MCP execution: {string.Join(" | ", dialogMessages)}")
    {
        DialogMessages = dialogMessages;
    }
}

// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.Components.Properties;

/// <summary>
/// The search of the property grid: which properties stay visible for a search text.
/// </summary>
public static class PropertyFilter
{
    /// <summary>
    /// Indicates whether a property stays visible for a search text: its name contains the text (ignoring case), or the name of one of
    /// its nested properties does, as in the WPF property grid.
    /// </summary>
    /// <param name="node">The property.</param>
    /// <param name="token">The search text. An empty or blank text shows every property.</param>
    /// <returns><c>true</c> if the property stays visible; otherwise, <c>false</c>.</returns>
    public static bool Matches(NodeViewModel node, string? token)
    {
        return string.IsNullOrWhiteSpace(token) || MatchesToken(node, token);
    }

    private static bool MatchesToken(NodeViewModel node, string token)
    {
        return node.DisplayName.Contains(token, StringComparison.CurrentCultureIgnoreCase) || node.Children.Any(x => MatchesToken(x, token));
    }
}

// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Stride.Core.Assets.Editor.Commands;

/// <summary>
/// How the shells lay out the commands of a location: in groups, each between separators.
/// </summary>
[Experimental("STREXP003")]
public static class CommandArrangement
{
    /// <summary>
    /// The commands placed at <paramref name="location"/>, in groups: the commands of a group by their order, then in
    /// registration order; the groups by the lowest order of their commands, then by their first command's registration.
    /// </summary>
    /// <param name="commands">The commands, in registration order.</param>
    /// <param name="location">The location.</param>
    public static IReadOnlyList<IReadOnlyList<CommandDescriptor>> Arrange(IEnumerable<CommandDescriptor> commands, string location)
    {
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentNullException.ThrowIfNull(location);

        var placed = commands
            .Select((command, index) => (Command: command, Placement: command.GetPlacement(location), Index: index))
            .Where(x => x.Placement is not null)
            .ToList();
        return placed
            .GroupBy(x => x.Placement!.Group ?? string.Empty, StringComparer.Ordinal)
            .Select(group => group.OrderBy(x => x.Placement!.Order).ThenBy(x => x.Index).ToList())
            .OrderBy(group => group[0].Placement!.Order).ThenBy(group => group.Min(x => x.Index))
            .Select(group => (IReadOnlyList<CommandDescriptor>)group.Select(x => x.Command).ToList())
            .ToList();
    }
}

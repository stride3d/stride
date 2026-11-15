// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Stride.Core.Assets.Editor.Commands;

/// <summary>
/// Where a command is shown.
/// </summary>
/// <param name="Location">The location: one of <see cref="CommandLocations"/>, or of another layer of the editor (the scene
/// editor's, for instance).</param>
/// <param name="Group">The group of the command at that location: the commands of a group are shown together, between
/// separators. Null for the default group of the location.</param>
/// <param name="Order">The order of the command in its group; the groups are ordered by the lowest order of their commands.</param>
[Experimental("STREXP003")]
public sealed record CommandPlacement(string Location, string? Group = null, int Order = 0);

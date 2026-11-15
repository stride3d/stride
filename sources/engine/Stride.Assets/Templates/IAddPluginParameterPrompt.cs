// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stride.Assets.Templates;

/// <summary>
/// Asks for the plugin's name (also its namespace) and, when several projects could reference it, which one does.
/// </summary>
public interface IAddPluginParameterPrompt
{
    /// <param name="referencingProjects">The projects that can take the reference, when more than one qualifies; empty otherwise.</param>
    Task<AddPluginResult?> PromptAsync(string defaultName, Func<string, bool> isNameTaken, IReadOnlyList<string> referencingProjects);
}

/// <param name="ReferencingProject">The chosen project, from the list offered; null when none was offered.</param>
public sealed record AddPluginResult(string PluginName, string? ReferencingProject);

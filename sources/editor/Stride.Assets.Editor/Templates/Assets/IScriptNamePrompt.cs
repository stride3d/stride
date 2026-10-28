// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Assets.Editor.Templates;

/// <summary>
/// The dialog of <see cref="ScriptTemplateGenerator"/>: asks the class name, the namespace and the template of a new script. The
/// toolkit provides it.
/// </summary>
public interface IScriptNamePrompt
{
    /// <summary>
    /// Shows the dialog and waits for the user to accept (the view model then holds valid names) or cancel.
    /// </summary>
    /// <param name="viewModel">The names to ask, with their proposed values.</param>
    /// <returns><c>true</c> when the user accepted, <c>false</c> when they cancelled.</returns>
    Task<bool> PromptAsync(ScriptNameViewModel viewModel);
}

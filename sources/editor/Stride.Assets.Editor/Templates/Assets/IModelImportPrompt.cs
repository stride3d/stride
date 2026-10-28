// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Assets.Editor.Templates;

/// <summary>
/// The dialog of <see cref="ModelFromFileTemplateGenerator"/>: asks what to import with the model (materials, textures,
/// animations, skeleton). The toolkit provides it.
/// </summary>
public interface IModelImportPrompt
{
    /// <summary>
    /// Shows the dialog and waits for the user to accept or cancel.
    /// </summary>
    /// <param name="viewModel">The options to ask, with their proposed values.</param>
    /// <returns><c>true</c> when the user accepted, <c>false</c> when they cancelled.</returns>
    Task<bool> PromptAsync(ImportModelFromFileViewModel viewModel);
}

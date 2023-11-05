// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Presentation.Services;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;

public class BrowseDirectoryCommand : ChangeValueWithPickerCommandBase
{
    /// <summary>
    /// The name of this command.
    /// </summary>
    public const string CommandName = "BrowseDirectory";

    private readonly IDialogService dialogService;
    private readonly IInitialDirectoryProvider? initialDirectoryProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="BrowseDirectoryCommand"/> class.
    /// </summary>
    /// <param name="dialogService">The dialog service used to pick the folder.</param>
    /// <param name="initialDirectoryProvider">An object that provide the initial directory to use in the picker.</param>
    public BrowseDirectoryCommand(IDialogService dialogService, IInitialDirectoryProvider? initialDirectoryProvider = null)
    {
        this.dialogService = dialogService;
        this.initialDirectoryProvider = initialDirectoryProvider;
    }

    /// <inheritdoc/>
    public override string Name => CommandName;

    /// <inheritdoc/>
    public override CombineMode CombineMode => CombineMode.AlwaysCombine;

    /// <inheritdoc/>
    public override bool CanAttach(INodePresenter nodePresenter)
    {
        return typeof(UDirectory).IsAssignableFrom(nodePresenter.Type);
    }

    /// <inheritdoc/>
    protected override async Task<PickerResult> ShowPicker(IReadOnlyCollection<INodePresenter> nodePresenters, object currentValue, object? parameter)
    {
        var currentPath = (UDirectory)currentValue;
        if (initialDirectoryProvider is not null)
        {
            currentPath = initialDirectoryProvider.GetInitialDirectory(currentPath);
        }

        var result = await dialogService.OpenFolderPickerAsync(currentPath);
        var pickerResult = new PickerResult
        {
            ProcessChange = result is not null,
            NewValue = result
        };
        return pickerResult;
    }
}

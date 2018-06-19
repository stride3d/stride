// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xenko.Core.IO;
using Xenko.Core.Presentation.Quantum;
using Xenko.Core.Presentation.Quantum.Presenters;
using Xenko.Core.Presentation.Services;
using Xenko.Core.Quantum;

namespace Xenko.Core.Assets.Editor.Quantum.NodePresenters.Commands
{
    public class BrowseDirectoryCommand : ChangeValueWithPickerCommandBase
    {
        /// <summary>
        /// The name of this command.
        /// </summary>
        public const string CommandName = "BrowseDirectory";

        private readonly IDialogService dialogService;
        private readonly IInitialDirectoryProvider initialDirectoryProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="BrowseDirectoryCommand"/> class.
        /// </summary>
        /// <param name="dialogService">The dialog service used to pick the folder.</param>
        /// <param name="initialDirectoryProvider">An object that provide the initial directory to use in the picker.</param>
        public BrowseDirectoryCommand(IDialogService dialogService, IInitialDirectoryProvider initialDirectoryProvider = null)
        {
            if (dialogService == null) throw new ArgumentNullException(nameof(dialogService));
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
        protected override async Task<PickerResult> ShowPicker(IReadOnlyCollection<INodePresenter> nodePresenters, object currentValue, object parameter)
        {
            var openDialog = dialogService.CreateFolderOpenModalDialog();
            var currentPath = (UDirectory)currentValue;
            if (initialDirectoryProvider != null)
            {
                currentPath = initialDirectoryProvider.GetInitialDirectory(currentPath);
            }
            if (currentPath != null)
            {
                openDialog.InitialDirectory = currentPath;
            }

            var result = await openDialog.ShowModal();
            var pickerResult = new PickerResult
            {
                ProcessChange = result == DialogResult.Ok,
                NewValue = result == DialogResult.Ok ? new UDirectory(openDialog.Directory) : null
            };
            return pickerResult;
        }
    }
}

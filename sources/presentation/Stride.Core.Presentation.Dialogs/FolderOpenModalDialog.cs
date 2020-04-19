// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.WindowsAPICodePack.Dialogs;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Services;

namespace Stride.Core.Presentation.Dialogs
{
    public class FolderOpenModalDialog : ModalDialogBase, IFolderOpenModalDialog
    {
        internal FolderOpenModalDialog([NotNull] IDispatcherService dispatcher)
            : base(dispatcher)
        {
            Dialog = new CommonOpenFileDialog { EnsurePathExists = true };
            OpenDlg.IsFolderPicker = true;
        }

        /// <inheritdoc/>
        public string Directory { get; private set; }

        /// <inheritdoc/>
        public string InitialDirectory { get { return OpenDlg.InitialDirectory; } set { OpenDlg.InitialDirectory = value.Replace('/', '\\'); } }

        private CommonOpenFileDialog OpenDlg => (CommonOpenFileDialog)Dialog;

        public override async Task<DialogResult> ShowModal()
        {
            await InvokeDialog();
            Directory = Result != DialogResult.Cancel ? OpenDlg.FileName : null;
            return Result;
        }
    }
}

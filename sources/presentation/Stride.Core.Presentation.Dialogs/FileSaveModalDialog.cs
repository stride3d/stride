// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAPICodePack.Dialogs;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Services;

namespace Stride.Core.Presentation.Dialogs
{
    public class FileSaveModalDialog : ModalDialogBase, IFileSaveModalDialog
    {
        internal FileSaveModalDialog([NotNull] IDispatcherService dispatcher)
            : base(dispatcher)
        {
            Dialog = new CommonSaveFileDialog();
            Filters = new List<FileDialogFilter>();
        }

        /// <inheritdoc/>
        public IList<FileDialogFilter> Filters { get; set; }

        /// <inheritdoc/>
        public string FilePath { get; private set; }

        /// <inheritdoc/>
        public string InitialDirectory { get { return SaveDlg.InitialDirectory; } set { SaveDlg.InitialDirectory = value; } }

        /// <inheritdoc/>
        public string DefaultFileName { get { return SaveDlg.DefaultFileName; } set { SaveDlg.DefaultFileName = value; } }

        /// <inheritdoc/>
        public string DefaultExtension { get { return SaveDlg.DefaultExtension; } set { SaveDlg.DefaultExtension = value; } }

        private CommonSaveFileDialog SaveDlg => (CommonSaveFileDialog)Dialog;

        /// <inheritdoc/>
        public override async Task<DialogResult> ShowModal()
        {
            SaveDlg.Filters.Clear();
            foreach (var filter in Filters.Where(x => !string.IsNullOrEmpty(x.ExtensionList)))
            {
                SaveDlg.Filters.Add(new CommonFileDialogFilter(filter.Description, filter.ExtensionList));
            }
            SaveDlg.AlwaysAppendDefaultExtension = true;
            await InvokeDialog();
            FilePath = Result != DialogResult.Cancel ? SaveDlg.FileName : null;
            return Result;
        }
    }
}

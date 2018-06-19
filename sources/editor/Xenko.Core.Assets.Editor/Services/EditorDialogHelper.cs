// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xenko.Core.Assets.Editor.Settings;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Extensions;
using Xenko.Core.IO;
using Xenko.Core.Presentation.Services;
using Xenko.Core.Presentation.ViewModel;

namespace Xenko.Core.Assets.Editor.Services
{
    public static class EditorDialogHelper
    {
        // TODO: Put ShowMessage here, find a way to reach the editor title from Xenko

        public static async Task<UFile> BrowseForExistingProject(IViewModelServiceProvider serviceProvider)
        {
            var initialDirectory = InternalSettings.FileDialogLastOpenSessionDirectory.GetValue();
            var extensions = string.Join(";", EditorViewModel.SolutionFileExtension, EditorViewModel.PackageFileExtension);
            var filters = new List<FileDialogFilter>
            {
                new FileDialogFilter("Solution or package files", extensions),
                new FileDialogFilter("Solution file", EditorViewModel.SolutionFileExtension),
                new FileDialogFilter("Package file", EditorViewModel.PackageFileExtension),
            };
            var filePaths = await OpenFileDialog(serviceProvider, false, initialDirectory, filters);
            return filePaths?.FirstOrDefault();
        }

        public static async Task<IEnumerable<UFile>> OpenFileDialog(IViewModelServiceProvider serviceProvider, bool allowMultiSelection, string initialDirectory, IEnumerable<FileDialogFilter> filters = null)
        {
            var dialogService = serviceProvider.Get<IDialogService>();
            IFileOpenModalDialog dlg = dialogService.CreateFileOpenModalDialog();
            dlg.InitialDirectory = initialDirectory;

            dlg.AllowMultiSelection = allowMultiSelection;
            if (filters != null)
            {
                filters.ForEach(x => dlg.Filters.Add(x));
            }
            else
            {
                dlg.Filters.Add(new FileDialogFilter("All Files", "*.*"));
            }

            return await dlg.ShowModal() == DialogResult.Ok ? dlg.FilePaths.Select(x => new UFile(x)) : null;
        }
    }
}

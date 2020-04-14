// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModel;

namespace Stride.Core.Assets.Editor.Services
{
    public static class EditorDialogHelper
    {
        // TODO: Put ShowMessage here, find a way to reach the editor title from Stride

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

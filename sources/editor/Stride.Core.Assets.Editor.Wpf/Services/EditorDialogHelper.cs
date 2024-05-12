// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Services
{
    public static class EditorDialogHelper
    {
        // TODO: Put ShowMessage here, find a way to reach the editor title from Stride

        public static async Task<UFile> BrowseForExistingProject(IViewModelServiceProvider serviceProvider)
        {
            var initialDirectory = InternalSettings.FileDialogLastOpenSessionDirectory.GetValue();
            var filters = new List<FilePickerFilter>
            {
                new("Solution or package files") { Patterns = [EditorViewModel.SolutionFileExtension, EditorViewModel.PackageFileExtension]},
                new("Solution file") { Patterns = [EditorViewModel.SolutionFileExtension]},
                new("Package file") { Patterns = [EditorViewModel.PackageFileExtension]},
            };
            var filePaths = await OpenFileDialog(serviceProvider, false, initialDirectory, filters);
            return filePaths?.FirstOrDefault();
        }

        public static async Task<IEnumerable<UFile>> OpenFileDialog(IViewModelServiceProvider serviceProvider, bool allowMultiSelection, string initialDirectory, IReadOnlyList<FilePickerFilter> filters = null)
        {
            if (allowMultiSelection)
            {
                var files = await serviceProvider.Get<IDialogService>().OpenMultipleFilesPickerAsync(initialDirectory, filters);
                return files.Count > 0 ? files : null;
            }
            else
            {
                var file = await serviceProvider.Get<IDialogService>().OpenFilePickerAsync(initialDirectory, filters);
                return file?.Yield();
            }
        }
    }
}

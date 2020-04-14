// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xenko.Core;
using Xenko.Core.IO;
using Xenko.Core.Presentation.Commands;
using Xenko.Core.Presentation.Services;
using Xenko.Core.Presentation.ViewModel;

namespace Xenko.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels
{
    public abstract class ProjectTemplateCollectionViewModel : TemplateDescriptionCollectionViewModel
    {
        private UDirectory location;

        protected ProjectTemplateCollectionViewModel(IViewModelServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            BrowseDirectoryCommand = new AnonymousTaskCommand<string>(serviceProvider, BrowseDirectory);
        }

        public UDirectory Location { get { return location; } set { SetValue(ref location, value); } }

        public ICommandBase BrowseDirectoryCommand { get; }

        public override bool ValidateProperties(out string error)
        {
            if (SelectedTemplate is TemplateDescriptionViewModel)
            {
                if (!UPath.IsValid(Location))
                {
                    error = "Invalid output directory.";
                    return false;
                }
                if (!UFile.IsValid(Name) || Name.Contains(UPath.DirectorySeparatorString) || Name.Contains(UPath.DirectorySeparatorStringAlt))
                {
                    error = "Invalid name.";
                    return false;
                }
                var outputPath = UPath.Combine<UDirectory>(Location, Name);
                if (Directory.Exists(outputPath))
                {
                    error = "Cannot use the selected name because a folder with the same name already exists in the same location.";
                    return false;
                }
            }
            error = "";
            return true;
        }

        protected string GenerateUniqueNameAtLocation(List<string> conflictingNames = null)
        {
            if (conflictingNames == null)
                conflictingNames = new List<string>();

            if (Directory.Exists(Location))
            {
                // Also add currently existing folders
                var existingFolders = Directory.GetDirectories(Location).Select(Path.GetFileName);
                conflictingNames.AddRange(existingFolders);
            }
            // Generate a name that does not collide with one of the previously gathered names
            var newName = NamingHelper.ComputeNewName(SelectedTemplate.DefaultOutputName, conflictingNames, x => x, "{0}{1}");
            return newName;
        }

        private async Task BrowseDirectory(string variableName)
        {
            IFolderOpenModalDialog openDialog = ServiceProvider.Get<IDialogService>().CreateFolderOpenModalDialog();
            openDialog.InitialDirectory = Location;
            var result = await openDialog.ShowModal();
            if (result == DialogResult.Ok)
            {
                UDirectory directory = openDialog.Directory;
                var property = GetType().GetProperty(variableName);
                property.SetValue(this, directory);
            }
        }
    }
}

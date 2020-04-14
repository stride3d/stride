// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

using Xenko.Core.Assets;
using Xenko.Core.Assets.Editor.View;
using Xenko.Core.Assets.Templates;
using Xenko.Core;
using Xenko.Core.Presentation.Services;
using Xenko.Core.Presentation.Windows;
using Xenko.Assets.Templates;
using Xenko.Core.Assets.Editor.Components.TemplateDescriptions;
using Xenko.Core.Extensions;

namespace Xenko.Assets.Presentation.Templates
{
    /// <summary>
    /// Generator to create a library and add it to the selected package.
    /// </summary>
    public class ProjectLibraryTemplateGenerator : SessionTemplateGenerator
    {
        public static readonly ProjectLibraryTemplateGenerator Default = new ProjectLibraryTemplateGenerator();

        public static readonly Guid TemplateId = new Guid("e12246ff-41a1-49d4-90e4-e72a4eb4a3e9");

        private static List<string> ExtractReferencesList(SessionTemplateGeneratorParameters parameters)
        {
            // libraries and executables
            var referencedBinaryNames = new List<string>();
            referencedBinaryNames.AddRange(parameters.Session.Projects.OfType<SolutionProject>().Where(x => x.FullPath != null).Select(x => x.FullPath.GetFileNameWithoutExtension()));
            return referencedBinaryNames;
        }

        private static bool IsNameColliding(IEnumerable<string> packageRefNames, string modifiedName)
        {
            return packageRefNames.Any(pp => string.Equals(pp, modifiedName, StringComparison.OrdinalIgnoreCase));
        }

        public override bool IsSupportingTemplate(TemplateDescription templateDescription)
        {
            if (templateDescription == null) throw new ArgumentNullException(nameof(templateDescription));
            return templateDescription.Id == TemplateId;
        }

        public override async Task<bool> PrepareForRun(SessionTemplateGeneratorParameters parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            parameters.Validate();

            var initialName = parameters.Name;
            var existingNames = ExtractReferencesList(parameters);

            initialName = NamingHelper.ComputeNewName(initialName, (Core.IO.UFile uf) => IsNameColliding(existingNames, uf), "{0}{1}");
            var window = new ProjectLibraryWindow(initialName);
            window.LibNameInputValidator = (name) => IsNameColliding(existingNames, name);

            await window.ShowModal();

            if (window.Result == DialogResult.Cancel)
                return false;

            parameters.Name = Utilities.BuildValidProjectName(window.LibraryName);
            parameters.Namespace = Utilities.BuildValidNamespaceName(window.Namespace);

            var collision = IsNameColliding(existingNames, parameters.Name);
            return !collision;  // we cannot allow to flow the creation request in case of name collision, because the underlying viewmodel system does not have protection against it.
        }

        protected sealed override bool Generate(SessionTemplateGeneratorParameters parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            parameters.Validate();

            var logger = parameters.Logger;
            var name = parameters.Name;
            var session = parameters.Session;

            // Log progress
            var projectName = name;
            ProjectTemplateGeneratorHelper.Progress(logger, $"Generating {projectName}...", 0, 1);

            // Generate the library
            List<string> generatedFiles;
            ProjectTemplateGeneratorHelper.AddOption(parameters, "Platforms", AssetRegistry.SupportedPlatforms);
            var project = ProjectTemplateGeneratorHelper.GenerateTemplate(parameters, "ProjectLibrary/ProjectLibrary.ttproj", projectName, PlatformType.Shared, null, ProjectType.Library, out generatedFiles);

            session.Projects.Add(project);

            // Load missing references
            session.LoadMissingReferences(parameters.Logger);

            // Log done
            ProjectTemplateGeneratorHelper.Progress(logger, "Done", 1, 1);
            return true;
        }
    }
}

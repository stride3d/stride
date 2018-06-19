// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Xenko.Core.Assets.Editor.Settings;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Assets.Templates;
using Xenko.Core.Extensions;
using Xenko.Core.IO;

namespace Xenko.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels
{
    public class UpdatePackageTemplateCollectionViewModel : ProjectTemplateCollectionViewModel
    {
        private readonly SessionViewModel session;

        public UpdatePackageTemplateCollectionViewModel(SessionViewModel session)
            : base(session.SafeArgument(nameof(session)).ServiceProvider)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            this.session = session;

            var rootGroup = new TemplateDescriptionGroupViewModel(ServiceProvider, "All templates");

            foreach (TemplateDescription template in session.FindTemplates(TemplateScope.Package))
            {
                var viewModel = new PackageTemplateViewModel(ServiceProvider, template, session);
                rootGroup.Templates.Add(viewModel);
            }

            Location = session.SolutionPath?.GetFullDirectory() ?? InternalSettings.TemplatesWindowDialogLastNewSessionTemplateDirectory.GetValue();
            if (string.IsNullOrWhiteSpace(Location))
                Location = UPath.Combine<UDirectory>(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Xenko Projects");

            SelectedGroup = rootGroup;
        }

        public override IEnumerable<TemplateDescriptionGroupViewModel> RootGroups => Enumerable.Empty<TemplateDescriptionGroupViewModel>();

        protected override string UpdateNameFromSelectedTemplate()
        {
            // Get package names in the current session
            return GenerateUniqueNameAtLocation(session.AllPackages.Select(x => x.Name).ToList());
        }
    }
}

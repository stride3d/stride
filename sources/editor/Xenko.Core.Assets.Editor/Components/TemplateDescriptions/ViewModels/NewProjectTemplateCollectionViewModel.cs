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
using Xenko.Core.Presentation.ViewModel;

namespace Xenko.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels
{
    public class NewProjectTemplateCollectionViewModel : ProjectTemplateCollectionViewModel
    {
        private readonly TemplateDescriptionGroupViewModel rootGroup;
        private bool arePropertiesValid;

        public NewProjectTemplateCollectionViewModel(IViewModelServiceProvider serviceProvider, SessionViewModel session)
            : base(serviceProvider)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            Session = session;

            rootGroup = new TemplateDescriptionGroupViewModel(serviceProvider, "All templates");

            // Add a default General group
            var defaultGroup = new TemplateDescriptionGroupViewModel(rootGroup, "General");
            
            foreach (TemplateDescription template in session.FindTemplates(TemplateScope.Session))
            {
                if (!IsAssetsOnlyTemplate(template))
                    continue;

                var viewModel = new PackageTemplateViewModel(serviceProvider, template, session);
                var group = ProcessGroup(rootGroup, template.Group) ?? defaultGroup;
                group.Templates.Add(viewModel);
            }

            Location = session.SolutionPath.GetFullDirectory() ?? InternalSettings.TemplatesWindowDialogLastNewSessionTemplateDirectory.GetValue();
            if (string.IsNullOrWhiteSpace(Location))
                Location = UPath.Combine<UDirectory>(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Xenko Projects");

            SelectedGroup = rootGroup;
        }

        private bool IsAssetsOnlyTemplate(TemplateDescription template)
        {
            // TODO We only have two such template for now, so check directly, maybe improve later
            return template.FullPath.FullPath.EndsWith("ProjectLibrary.xktpl")
                || template.FullPath.FullPath.EndsWith("ProjectExecutable.xktpl");
        }

        public SessionViewModel Session { get; }

        public bool ArePropertiesValid { get { return arePropertiesValid; } set { SetValue(ref arePropertiesValid, value); } }

        public override IEnumerable<TemplateDescriptionGroupViewModel> RootGroups => rootGroup.Yield();

        public override bool ValidateProperties(out string error)
        {
            return ArePropertiesValid = base.ValidateProperties(out error);
        }

        protected override string UpdateNameFromSelectedTemplate()
        {
            // Get package names in the current session
            return GenerateUniqueNameAtLocation(Session.AllPackages.Select(x => x.Name).ToList());
        }
    }
}

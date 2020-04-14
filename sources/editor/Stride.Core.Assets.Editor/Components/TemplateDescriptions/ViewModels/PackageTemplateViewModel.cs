// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Assets.Templates;
using Xenko.Core.Presentation.Commands;
using Xenko.Core.Presentation.ViewModel;

namespace Xenko.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels
{
    public class PackageTemplateViewModel : TemplateDescriptionViewModel
    {
        private readonly SessionViewModel session;

        public PackageTemplateViewModel(IViewModelServiceProvider serviceProvider, TemplateDescription template, SessionViewModel session = null)
            : base(serviceProvider, template)
        {
            this.session = session;
            UpdatePackageCommand = new AnonymousTaskCommand(ServiceProvider, UpdatePackage) { IsEnabled = session != null };
        }

        public ICommandBase UpdatePackageCommand { get; private set; }

        private Task UpdatePackage()
        {
            return session?.UpdatePackageTemplate(Template);
        }
    }
}

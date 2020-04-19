// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Templates;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModel;

namespace Stride.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels
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

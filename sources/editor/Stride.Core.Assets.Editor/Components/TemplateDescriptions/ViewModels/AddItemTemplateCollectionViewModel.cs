// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Xenko.Core.Extensions;
using Xenko.Core.Presentation.ViewModel;

namespace Xenko.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels
{
    public class AddItemTemplateCollectionViewModel : TemplateDescriptionCollectionViewModel
    {
        private string searchToken;

        public AddItemTemplateCollectionViewModel(IViewModelServiceProvider serviceProvider) : base(serviceProvider)
        {
            RootGroup = new TemplateDescriptionGroupViewModel(serviceProvider, "All templates");
        }

        public override IEnumerable<TemplateDescriptionGroupViewModel> RootGroups => RootGroup.Yield();

        public TemplateDescriptionGroupViewModel RootGroup { get; }

        public string SearchToken { get { return searchToken; } set { SetValue(ref searchToken, value, () => SelectedGroup = RootGroup); } }

        public override bool ValidateProperties(out string error)
        {
            error = "";
            return true;
        }

        protected override string UpdateNameFromSelectedTemplate()
        {
            return null;
        }

    }
}

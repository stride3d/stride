using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xenko.Core.Presentation.Quantum.View;
using Xenko.Core.Presentation.Quantum.ViewModels;
using Xenko.Core.Serialization;

namespace Xenko.Core.Assets.Editor.View.TemplateProviders
{
    public class UrlReferenceTemplateProvider : NodeViewModelTemplateProvider
    {
        public override string Name => "Url reference";

        public override bool MatchNode(NodeViewModel node)
        {
            var isReference = typeof(UrlReference).IsAssignableFrom(node.Type);
            return isReference;
        }
    }
}

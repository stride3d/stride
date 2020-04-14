// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Xenko.Core.Assets.Editor.Services;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Presentation.Quantum.Presenters;

namespace Xenko.Core.Assets.Editor.Quantum.NodePresenters.Updaters
{
    public sealed class DocumentationNodeUpdater : AssetNodePresenterUpdaterBase
    {
        private readonly UserDocumentationService documentationService;

        public DocumentationNodeUpdater([NotNull] UserDocumentationService documentationService)
        {
            this.documentationService = documentationService ?? throw new ArgumentNullException(nameof(documentationService));
        }

        protected override void UpdateNode(IAssetNodePresenter node)
        {
            if (!(node is MemberNodePresenter memberNode))
                return;

            if (node.Index.Value is PropertyKey propertyKey)
            {
                var propertyKeyDocumentation = documentationService.GetPropertyKeyDocumentation(propertyKey);
                if (propertyKeyDocumentation != null)
                    node.AttachedProperties.Add(DocumentationData.Key, propertyKeyDocumentation);
            }
            else
            {
                var memberDocumentation = documentationService.GetMemberDocumentation(memberNode.MemberDescriptor, node.Root.Type);
                if (memberDocumentation != null)
                {
                    node.AttachedProperties.Add(DocumentationData.Key, memberDocumentation);
                }
            }
        }
    }
}

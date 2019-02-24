using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xenko.Core.Assets.Editor.Services;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Presentation.Quantum.Presenters;

namespace Xenko.Core.Assets.Editor.Quantum.NodePresenters.Commands
{
    public class PickupAssetUrlCommand : PickupAssetCommand
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="PickupAssetUrlCommand"/> class.
        /// </summary>
        /// <param name="session">The current session.</param>
        public PickupAssetUrlCommand(SessionViewModel session) : base(session)
        {
        }

        /// <inheritdoc />
        public override bool CanAttach(INodePresenter nodePresenter)
        {
            return UrlReferenceHelper.ContainsUrlReferenceType(nodePresenter.Descriptor);
        }

        /// <inheritdoc />
        protected override bool FilterAsset(AssetViewModel asset, Type referenceType)
        {
            var targetType = UrlReferenceHelper.GetTargetType(referenceType);

            if (targetType == null) return true;

            var contentType = AssetRegistry.GetContentType(asset.AssetType);

            return contentType == targetType;
        }

        protected override AssetViewModel GetCurrentTarget(object currentValue)
        {
            return UrlReferenceHelper.GetReferenceTarget(Session, currentValue);
        }

        protected override IEnumerable<Type> GetAssetTypes(Type contentType)
        {
            var targetType = UrlReferenceHelper.GetTargetType(contentType);

            if (targetType == null) return AssetRegistry.GetPublicTypes();

            return AssetRegistry.GetAssetTypes(targetType);
        }

        protected override object CreateReference(AssetViewModel asset, Type referenceType)
        {
            return UrlReferenceHelper.CreateReference(asset, referenceType);
        }
    }
}

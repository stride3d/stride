using System.Threading.Tasks;
using Stride.Core.BuildEngine;
using Stride.Core.Annotations;
using Stride.Core.Serialization.Contents;

namespace Stride.Core.Assets.Compiler
{
    /// <summary>
    /// An <see cref="AssetCommand"/> that will create a default instance of the content type for a given asset, rather than compiling it.
    /// </summary>
    /// <typeparam name="TAsset">The type of asset for which to generate a default instance of content.</typeparam>
    /// <typeparam name="TContent">The type of content to generate.</typeparam>
    public class DummyAssetCommand<TAsset, TContent> : AssetCommand<TAsset> where TAsset : Asset where TContent : new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DummyAssetCommand{TAsset, TContent}"/> class.
        /// </summary>
        /// <param name="assetItem">The asset to compile.</param>
        public DummyAssetCommand([NotNull] AssetItem assetItem)
            : base(assetItem.Location, (TAsset)assetItem.Asset, assetItem.Package)
        {
        }

        protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
        {
            var contentManager = new ContentManager(MicrothreadLocalDatabases.ProviderService);
            var dummyObject = new TContent();
            contentManager.Save(Url, dummyObject);
            return Task.FromResult(ResultStatus.Successful);
        }
    }
}

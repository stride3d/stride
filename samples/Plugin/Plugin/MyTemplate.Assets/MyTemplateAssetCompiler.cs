using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core.Serialization.Contents;

namespace MyTemplate.Assets;

[AssetCompiler(typeof(MyTemplateAsset), typeof(AssetCompilationContext))]
public class MyTemplateAssetCompiler : AssetCompilerBase
{
    protected override void Prepare(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
    {
        result.BuildSteps = new AssetBuildStep(assetItem);
        // An asset being compiled belongs to a package
        result.BuildSteps.Add(new MyTemplateCommand(targetUrlInStorage, (MyTemplateAsset)assetItem.Asset, assetItem.Package!));
    }

    private class MyTemplateCommand : AssetCommand<MyTemplateAsset>
    {
        public MyTemplateCommand(string url, MyTemplateAsset parameters, IAssetFinder assetFinder)
            : base(url, parameters, assetFinder)
        {
        }

        protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
        {
            var contentManager = new ContentManager(MicrothreadLocalDatabases.ProviderService);
            contentManager.Save(Url, new MyTemplateData { Speed = Parameters.Speed });
            return Task.FromResult(ResultStatus.Successful);
        }
    }
}

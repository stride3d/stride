using Stride.Core.Serialization;

namespace Stride.Core.Assets.Tests.Compilers;

public class CompilerTestBase : IDisposable
{
    public CompilerTestBase()
    {
        TestCompilerBase.CompiledAssets = [];
    }

    public void Dispose()
    {
        TestCompilerBase.CompiledAssets = null!;
        GC.SuppressFinalize(this);
    }

    protected static TContentType CreateRef<TContentType>(AssetItem assetItem) where TContentType : class, new()
    {
        return AttachedReferenceManager.CreateProxyObject<TContentType>(assetItem.Id, assetItem.Location);
    }
}

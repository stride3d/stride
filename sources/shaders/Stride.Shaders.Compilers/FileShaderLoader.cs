using Stride.Core.IO;
using Stride.Core.Storage;
using Stride.Shaders.Spirv.Building;

namespace Stride.Shaders.Compilers;

public class FileShaderLoader(IVirtualFileProvider FileProvider) : ShaderLoaderBase(new FileShaderCache(FileProvider))
{
    public ShaderSourceManager SourceManager { get; } = new(FileProvider);

    protected override bool ExternalFileExists(string name) => SourceManager.IsClassExists(name);

    public override bool LoadExternalFileContent(string name, out string filename, out string code, out ObjectId hash)
    {
        var result = SourceManager.LoadShaderSource(name);
        filename = result.Path;
        code = result.Source;
        hash = result.Hash;

        return true;
    }
}

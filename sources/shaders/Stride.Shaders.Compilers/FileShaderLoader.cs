using Stride.Core.IO;
using Stride.Core.Storage;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Building;

namespace Stride.Shaders.Compilers;

public class FileShaderLoader : ShaderLoaderBase
{
    public ShaderSourceManager SourceManager { get; }

    public FileShaderLoader(IVirtualFileProvider fileProvider) : base(new FileShaderCache(VirtualFileSystem.ApplicationCache))
    {
        SourceManager = new ShaderSourceManager(fileProvider);
        // Wire up the importer so the file cache can resolve struct types during deserialization
        if (Cache is FileShaderCache fsc)
            fsc.ShaderImporter = new ShaderLoaderImporter(this);
    }

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

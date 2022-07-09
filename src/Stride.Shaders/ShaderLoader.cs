namespace Stride.Shaders;

public class ShaderLoader
{
    ShaderSourceManager ShaderResource = new();

    public ShaderLoader(string path)
    {
        ShaderResource.AddDirectory(path);
    }

    public string Get(string name)
    {
        return ShaderResource.GetShaderSource(name);
    }
    
}
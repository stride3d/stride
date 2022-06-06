namespace Stride.Shaders;

public struct ShaderResult<T,E>
{
    private readonly T? value;
    private readonly E? error;

    public bool isOk;

    public ShaderResult(T v)
    {
        value = v;
        error = default;
        isOk = true;
    }
    public ShaderResult(E e)
    {
        error = e;
        value = default;
        isOk = false;
    }
}

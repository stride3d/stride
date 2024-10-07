namespace Stride.Shaders.Spirv.Core.Validation;


public class Validation
{
    public List<ValidationPass> Passes;

    Validation()
    {
        Passes = new();
    }
}
namespace Stride.Shaders.Spirv.Core;

public interface IWrapperInstruction
{
    Instruction Inner { get; set; }
}
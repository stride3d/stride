using SoftTouch.Spirv;

namespace SDSL.Mixing;

public partial class ShaderMixer
{
    public ShaderMixer With(VariableData variable)
    {
        return variable.TypeInfo.Scope switch 
        {
            VariableScope.Input => (ShaderMixer)WithInput(variable),
            VariableScope.Output => (ShaderMixer)WithOutput(variable),
            VariableScope.Uniform => (ShaderMixer)WithUniform(variable),
            _ => throw new NotImplementedException()
        };
    }
}
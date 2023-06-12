using SoftTouch.Spirv;

namespace SDSL.Mixer;

public partial struct Mixer
{
    public Mixer With(VariableData variable)
    {
        return variable.TypeInfo.Scope switch 
        {
            VariableScope.Input => WithInput(variable),
            VariableScope.Output => WithOutput(variable),
            VariableScope.Uniform => WithUniform(variable),
            _ => throw new NotImplementedException()
        };
    }
    public Mixer WithInput(VariableData variable)
    {
        throw new NotImplementedException();
        return this;
    }
    public Mixer WithOutput(VariableData variable)
    {
        throw new NotImplementedException();
        return this;
    }
    public Mixer WithUniform(VariableData variable)
    {
        throw new NotImplementedException();
        return this;
    }
}
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Core.Parsing;
using static Spv.Specification;

namespace Stride.Shaders.Spirv;


public delegate void ConditionBody(FunctionBuilder function);
public delegate Instruction ConditionCheck(FunctionBuilder function);

public ref struct ConditionalBuilder
{
    Mixer mixer;
    FunctionBuilder builder;

    public MutRefInstruction lastLabel;

    public void If(ConditionCheck condition, ConditionBody cf)
    {
        // TODO: prepare condition
        cf.Invoke(builder);
    }
    public void ElseIf(ConditionCheck condition, ConditionBody cf)
    {
        // TODO: replace last 
        // TODO: prepare condition
        cf.Invoke(builder);
    }
    public void Else(ConditionCheck condition, ConditionBody cf)
    {
        // TODO: replace last 
        // TODO: prepare condition
        cf.Invoke(builder);
    }

    public void Finish()
    {
        //TODO : Finish conditions
    }
}

public ref partial struct FunctionBuilder
{

}
using System.Numerics;
using System.Security.Cryptography;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using static Stride.Shaders.Spirv.Core.Buffers.MultiBuffer;
using static Spv.Specification;

namespace Stride.Shaders.Spirv;

/// <summary>
/// Spirv Mixer object mainly designed around SDSL
/// </summary>
public sealed partial class Mixer : MixerBase
{
    //public FunctionFinder Functions => new(this);
    //FunctionBuffer functions;

    public MultiBufferLocalVariables LocalVariables => Buffer.LocalVariables;
    public MultiBufferGlobalVariables GlobalVariables => Buffer.GlobalVariables;




    public static Inheritance Create(string name)
    {
        return new(new(name));
    }

    public Mixer(string name) : base(name)
    {
        Buffer.AddOpMemoryModel(AddressingModel.Logical, MemoryModel.GLSL450);
        //buffer.AddOpExtension("SPV_GOOGLE_decorate_string");
    }

    public Mixer WithCapability(Capability capability)
    {
        Buffer.AddOpCapability(capability); 
        return this;
    }

    public EntryPoint WithEntryPoint(ExecutionModel model, string name)
    {
        return new EntryPoint(this, model, name);
    }
    public FunctionBuilder WithFunction(string type, string name, FunctionBuilder.CreateFunctionParameters parameterCreate)
    {
        return new FunctionBuilder(this, type, name, parameterCreate);
    }

    public Mixer Inherit(string mixin)
    {
        Mixins.Add(mixin);
        Buffer.AddOpSDSLMixinInherit(mixin);
        return this;
    }

    public Mixer WithType(string type, StorageClass? storage = null)
    {
        if (type.Contains('*'))
            CreateTypePointer(type.AsMemory(), storage ?? throw new Exception("storage should not be null"));
        else 
            GetOrCreateBaseType(type.AsMemory());
        return this;
    }

    public Mixer WithInput(string type, string name, string semantic, ExecutionModel execution)
    {
        var t_variable = GetOrCreateBaseType(type.AsMemory());
        var p_t_variable = Buffer.AddOpTypePointer(StorageClass.Input, t_variable.ResultId ?? -1);
        Buffer.AddOpSDSLIOVariable(p_t_variable.ResultId ?? -1, StorageClass.Input, execution, name, semantic, null);
        return this;
    }
    public Mixer WithOutput(string type, string name, string semantic, ExecutionModel execution)
    {
        var t_variable = GetOrCreateBaseType(type.AsMemory());
        var p_t_variable = Buffer.AddOpTypePointer(StorageClass.Output, t_variable.ResultId ?? -1);
        Buffer.AddOpSDSLIOVariable(p_t_variable.ResultId ?? -1, StorageClass.Output, execution, name, semantic, null);
        return this;
    }

    public Mixer WithConstant<T>(string name, T value)
        where T : struct
    {
        CreateConstant(name, value);
        return this;
    }
    public MixinInstruction CreateConstant<T>(T value)
        where T : struct
    {
        return value switch
        {
            sbyte v => Buffer.AddOpConstant<MultiBuffer, LiteralInteger>(GetOrCreateBaseType("sbyte".AsMemory()).ResultId ?? -1, v),
            short v => Buffer.AddOpConstant<MultiBuffer, LiteralInteger>(GetOrCreateBaseType("short".AsMemory()).ResultId ?? -1, v),
            int v => Buffer.AddOpConstant<MultiBuffer, LiteralInteger>(GetOrCreateBaseType("int".AsMemory()).ResultId ?? -1, v),
            long v => Buffer.AddOpConstant<MultiBuffer, LiteralInteger>(GetOrCreateBaseType("long".AsMemory()).ResultId ?? -1, v),
            byte v => Buffer.AddOpConstant<MultiBuffer, LiteralInteger>(GetOrCreateBaseType("byte".AsMemory()).ResultId ?? -1, v),
            ushort v => Buffer.AddOpConstant<MultiBuffer, LiteralInteger>(GetOrCreateBaseType("ushort".AsMemory()).ResultId ?? -1, v),
            uint v => Buffer.AddOpConstant<MultiBuffer, LiteralInteger>(GetOrCreateBaseType("uint".AsMemory()).ResultId ?? -1, v),
            ulong v => Buffer.AddOpConstant<MultiBuffer, LiteralInteger>(GetOrCreateBaseType("ulong".AsMemory()).ResultId ?? -1, v),
            float v => Buffer.AddOpConstant<MultiBuffer, LiteralFloat>(GetOrCreateBaseType("float".AsMemory()).ResultId ?? -1, v),
            double v => Buffer.AddOpConstant<MultiBuffer, LiteralFloat>(GetOrCreateBaseType("double".AsMemory()).ResultId ?? -1, v),
            Vector2 v => CreateConstantVector(v),
            Vector3 v => CreateConstantVector(v),
            Vector4 v => CreateConstantVector(v),
            _ => throw new NotImplementedException()
        };
    }
    public MixinInstruction CreateConstantVector<T>(T value)
        where T : struct
    {
        if (value is Vector2 vec2)
        {
            var t_const = GetOrCreateBaseType("float".AsMemory());
            var t_const2 = GetOrCreateBaseType("float2".AsMemory());

            var c1 = Buffer.AddOpConstant<MultiBuffer, LiteralFloat>(t_const.ResultId ?? -1, vec2.X);
            var c2 = Buffer.AddOpConstant<MultiBuffer, LiteralFloat>(t_const.ResultId ?? -1, vec2.Y);
            var cons = Buffer.AddOpConstantComposite(t_const2.ResultId ?? -1, stackalloc IdRef[] { c1.ResultId ?? -1, c2.ResultId ?? -1 });
            return cons;
        }
        else if (value is Vector3 vec3)
        {
            var t_const = GetOrCreateBaseType("float".AsMemory());
            var t_const2 = GetOrCreateBaseType("float3".AsMemory());

            var c1 = Buffer.AddOpConstant<MultiBuffer, LiteralFloat>(t_const.ResultId ?? -1, vec3.X);
            var c2 = Buffer.AddOpConstant<MultiBuffer, LiteralFloat>(t_const.ResultId ?? -1, vec3.Y);
            var c3 = Buffer.AddOpConstant<MultiBuffer, LiteralFloat>(t_const.ResultId ?? -1, vec3.Z);
            var cons = Buffer.AddOpConstantComposite(t_const2.ResultId ?? -1, stackalloc IdRef[] { c1.ResultId ?? -1, c2.ResultId ?? -1, c3.ResultId ?? -1 });
            return cons;
        }
        else if (value is Vector4 vec4)
        {
            var t_const = GetOrCreateBaseType("float".AsMemory());
            var t_const2 = GetOrCreateBaseType("float4".AsMemory());

            var c1 = Buffer.AddOpConstant<MultiBuffer, LiteralFloat>(t_const.ResultId ?? -1, vec4.X);
            var c2 = Buffer.AddOpConstant<MultiBuffer, LiteralFloat>(t_const.ResultId ?? -1, vec4.Y);
            var c3 = Buffer.AddOpConstant<MultiBuffer, LiteralFloat>(t_const.ResultId ?? -1, vec4.Z);
            var c4 = Buffer.AddOpConstant<MultiBuffer, LiteralFloat>(t_const.ResultId ?? -1, vec4.W);
            var cons = Buffer.AddOpConstantComposite(t_const2.ResultId ?? -1, stackalloc IdRef[] { c1.ResultId ?? -1, c2.ResultId ?? -1, c3.ResultId ?? -1, c4.ResultId ?? -1 });
            return cons;
        }
        throw new NotImplementedException();
    }
    public MixinInstruction CreateConstant<T>(string name, T value)
        where T : struct
    {
        if (value is sbyte vi8)
        {
            var t_const = GetOrCreateBaseType("sbyte".AsMemory());
            var cons = Buffer.AddOpConstant<MultiBuffer, LiteralInteger>(t_const.ResultId ?? -1, vi8);
            return cons;
        }
        else if (value is short vi16)
        {
            var t_const = GetOrCreateBaseType("short".AsMemory());
            var cons = Buffer.AddOpConstant<MultiBuffer, LiteralInteger>(t_const.ResultId ?? -1, vi16);
            return cons;
        }
        else if (value is int vi32)
        {
            var t_const = GetOrCreateBaseType("int".AsMemory());
            var cons = Buffer.AddOpConstant<MultiBuffer, LiteralInteger>(t_const.ResultId ?? -1, vi32);
            return cons;
        }
        else if (value is long vi64)
        {
            var t_const = GetOrCreateBaseType("long".AsMemory());
            var cons = Buffer.AddOpConstant<MultiBuffer, LiteralInteger>(t_const.ResultId ?? -1, vi64);
            return cons;
        }
        else if (value is byte vu8)
        {
            var t_const = GetOrCreateBaseType("byte".AsMemory());
            var cons = Buffer.AddOpConstant<MultiBuffer, LiteralInteger>(t_const.ResultId ?? -1, vu8);
            return cons;
        }
        else if (value is ushort vu16)
        {
            var t_const = GetOrCreateBaseType("ushort".AsMemory());
            var cons = Buffer.AddOpConstant<MultiBuffer, LiteralInteger>(t_const.ResultId ?? -1, vu16);
            return cons;
        }
        else if (value is uint vu32)
        {
            var t_const = GetOrCreateBaseType("uint".AsMemory());
            var cons = Buffer.AddOpConstant<MultiBuffer, LiteralInteger>(t_const.ResultId ?? -1, vu32);
            return cons;
        }
        else if (value is ulong vu64)
        {
            var t_const = GetOrCreateBaseType("ulong".AsMemory());
            var cons = Buffer.AddOpConstant<MultiBuffer, LiteralInteger>(t_const.ResultId ?? -1, vu64);
            return cons;
        }
        else if (value is float vf32)
        {
            var t_const = GetOrCreateBaseType("float".AsMemory());
            var cons = Buffer.AddOpConstant<MultiBuffer, LiteralFloat>(t_const.ResultId ?? -1, vf32);
            return cons;
        }
        else if (value is double vf64)
        {
            var t_const = GetOrCreateBaseType("double".AsMemory());
            var cons = Buffer.AddOpConstant<MultiBuffer, LiteralFloat>(t_const.ResultId ?? -1, vf64);
            return cons;
        }
        else if (value is Vector2 vec2)
        {
            var t_const = GetOrCreateBaseType("float".AsMemory());
            var t_const2 = GetOrCreateBaseType("float2".AsMemory());

            var c1 = Buffer.AddOpConstant<MultiBuffer, LiteralFloat>(t_const.ResultId ?? -1, vec2.X);
            var c2 = Buffer.AddOpConstant<MultiBuffer, LiteralFloat>(t_const.ResultId ?? -1, vec2.Y);
            var cons = Buffer.AddOpConstantComposite(t_const2.ResultId ?? -1, stackalloc IdRef[] { c1.ResultId ?? -1, c2.ResultId ?? -1 });
            return cons;
        }
        else if (value is Vector3 vec3)
        {
            var t_const = GetOrCreateBaseType("float".AsMemory());
            var t_const2 = GetOrCreateBaseType("float3".AsMemory());

            var c1 = Buffer.AddOpConstant<MultiBuffer, LiteralFloat>(t_const.ResultId ?? -1, vec3.X);
            var c2 = Buffer.AddOpConstant<MultiBuffer, LiteralFloat>(t_const.ResultId ?? -1, vec3.Y);
            var c3 = Buffer.AddOpConstant<MultiBuffer, LiteralFloat>(t_const.ResultId ?? -1, vec3.Z);
            var cons = Buffer.AddOpConstantComposite(t_const2.ResultId ?? -1, stackalloc IdRef[] { c1.ResultId ?? -1, c2.ResultId ?? -1, c3.ResultId ?? -1 });
            return cons;
        }
        else if (value is Vector4 vec4)
        {
            var t_const = GetOrCreateBaseType("float".AsMemory());
            var t_const2 = GetOrCreateBaseType("float4".AsMemory());

            var c1 = Buffer.AddOpConstant<MultiBuffer, LiteralFloat>(t_const.ResultId ?? -1, vec4.X);
            var c2 = Buffer.AddOpConstant<MultiBuffer, LiteralFloat>(t_const.ResultId ?? -1, vec4.Y);
            var c3 = Buffer.AddOpConstant<MultiBuffer, LiteralFloat>(t_const.ResultId ?? -1, vec4.Z);
            var c4 = Buffer.AddOpConstant<MultiBuffer, LiteralFloat>(t_const.ResultId ?? -1, vec4.W);
            var cons = Buffer.AddOpConstantComposite(t_const2.ResultId ?? -1, stackalloc IdRef[] { c1.ResultId ?? -1, c2.ResultId ?? -1, c3.ResultId ?? -1, c4.ResultId ?? -1 });
            return cons;
        }

        throw new NotImplementedException();
    }
    public override string ToString()
    {
        return Disassembler.Disassemble(new SortedWordBuffer(Buffer));
    }
}
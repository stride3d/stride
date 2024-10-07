using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using static Stride.Shaders.Spirv.Mixer;
using static Spv.Specification;

namespace Stride.Shaders.Spirv;

public ref struct FunctionCallerParameters
{
    public FunctionBuilder Builder {get; private set;}
    readonly Mixer mixer => Builder.mixer;
    Span<IdRef> inner;
    public Span<IdRef> ParameterVariables => inner[..Length];
    public int Length { get; private set; }

    public FunctionCallerParameters(FunctionBuilder builder, Span<IdRef> array)
    {
        if (array.Length != 16)
            throw new ArgumentException("Length must be 16");
        Builder = builder;
        inner = array;
        Length = 0;
    }

    public FunctionCallerParameters With(Instruction value)
    {
        //var p = mixer.Buffer.AddOpVariable(mixer.FindType(value.ResultType ?? -1), StorageClass.Function, null);
        //mixer.Buffer.AddOpStore(p, value, null);
        inner[Length] = value.ResultId ?? -1;
        Length += 1;
        return this;
    }
}

public delegate FunctionCallerParameters CreateCallFunctionParameter(FunctionCallerParameters p);

public ref partial struct FunctionBuilder
{
    public Instruction Call(string functionName, CreateCallFunctionParameter fp)
    {
        var parameters = new FunctionCallerParameters(this, stackalloc IdRef[16]);
        parameters = fp.Invoke(parameters);
        var function = mixer.Buffer.Functions[functionName][0];
        return mixer.Buffer.AddOpFunctionCall(function.ResultType ?? -1, function.ResultId ?? -1, parameters.ParameterVariables);
    }

    public FunctionBuilder CallFunction(string functionName, CreateCallFunctionParameter fp)
    {
        var parameters = new FunctionCallerParameters(this, stackalloc IdRef[16]);
        parameters = fp.Invoke(parameters);
        var function = mixer.Buffer.Functions[functionName][0];
        mixer.Buffer.AddOpFunctionCall(function.ResultType ?? -1, function.ResultId ?? -1, parameters.ParameterVariables);
        return this;
    }
    

    public FunctionBuilder CallFunction(string functionName)
    {
        var function = mixer.Buffer.Functions[functionName][0];
        mixer.Buffer.AddOpFunctionCall(function.ResultType ?? -1, function.ResultId ?? -1, Span<IdRef>.Empty);
        return this;
    }
    public FunctionBuilder CallFunction(string functionName, Instruction param1)
    {
        var var1 = mixer.Buffer.AddOpVariable(mixer.FindType(param1), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param1, null);
        var function = mixer.Buffer.Functions[functionName][0];
        mixer.Buffer.AddOpFunctionCall(function.ResultType ?? -1, function.ResultId ?? -1, stackalloc IdRef[1] { var1 });
        return this;
    }
    public FunctionBuilder CallFunction(string functionName, Instruction param1, Instruction param2)
    {
        var var1 = mixer.Buffer.AddOpVariable(mixer.FindType(param1), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param1, null);
        var var2 = mixer.Buffer.AddOpVariable(mixer.FindType(param2), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param2, null);

        var function = mixer.Buffer.Functions[functionName][0];
        mixer.Buffer.AddOpFunctionCall(function.ResultType ?? -1, function.ResultId ?? -1, stackalloc IdRef[] { var1, var2 });
        return this;
    }
    public FunctionBuilder CallFunction(
        string functionName,
        Instruction param1,
        Instruction param2,
        Instruction param3
    )
    {
        var var1 = mixer.Buffer.AddOpVariable(mixer.FindType(param1), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param1, null);
        var var2 = mixer.Buffer.AddOpVariable(mixer.FindType(param2), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param2, null);
        var var3 = mixer.Buffer.AddOpVariable(mixer.FindType(param3), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param3, null);

        var function = mixer.Buffer.Functions[functionName][0];
        mixer.Buffer.AddOpFunctionCall(function.ResultType ?? -1, function.ResultId ?? -1, stackalloc IdRef[] {
            var1,
            var2,
            var3
        });
        return this;
    }
    public FunctionBuilder CallFunction(
        string functionName,
        Instruction param1,
        Instruction param2,
        Instruction param3,
        Instruction param4
    )
    {
        var var1 = mixer.Buffer.AddOpVariable(mixer.FindType(param1), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param1, null);
        var var2 = mixer.Buffer.AddOpVariable(mixer.FindType(param2), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param2, null);
        var var3 = mixer.Buffer.AddOpVariable(mixer.FindType(param3), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param3, null);
        var var4 = mixer.Buffer.AddOpVariable(mixer.FindType(param4), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param4, null);

        var function = mixer.Buffer.Functions[functionName][0];
        mixer.Buffer.AddOpFunctionCall(function.ResultType ?? -1, function.ResultId ?? -1, stackalloc IdRef[] {
            var1,
            var2,
            var3,
            var4
        });
        return this;
    }
    public FunctionBuilder CallFunction(
        string functionName,
        Instruction param1,
        Instruction param2,
        Instruction param3,
        Instruction param4,
        Instruction param5
    )
    {
        var var1 = mixer.Buffer.AddOpVariable(mixer.FindType(param1), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param1, null);
        var var2 = mixer.Buffer.AddOpVariable(mixer.FindType(param2), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param2, null);
        var var3 = mixer.Buffer.AddOpVariable(mixer.FindType(param3), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param3, null);
        var var4 = mixer.Buffer.AddOpVariable(mixer.FindType(param4), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param4, null);
        var var5 = mixer.Buffer.AddOpVariable(mixer.FindType(param5), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param5, null);

        var function = mixer.Buffer.Functions[functionName][0];
        mixer.Buffer.AddOpFunctionCall(function.ResultType ?? -1, function.ResultId ?? -1, stackalloc IdRef[] {
            var1,
            var2,
            var3,
            var4,
            var5
        });
        return this;
    }
    public FunctionBuilder CallFunction(
        string functionName,
        Instruction param1,
        Instruction param2,
        Instruction param3,
        Instruction param4,
        Instruction param5,
        Instruction param6
    )
    {
        var var1 = mixer.Buffer.AddOpVariable(mixer.FindType(param1), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param1, null);
        var var2 = mixer.Buffer.AddOpVariable(mixer.FindType(param2), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param2, null);
        var var3 = mixer.Buffer.AddOpVariable(mixer.FindType(param3), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param3, null);
        var var4 = mixer.Buffer.AddOpVariable(mixer.FindType(param4), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param4, null);
        var var5 = mixer.Buffer.AddOpVariable(mixer.FindType(param5), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param5, null);
        var var6 = mixer.Buffer.AddOpVariable(mixer.FindType(param6), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param6, null);

        var function = mixer.Buffer.Functions[functionName][0];
        mixer.Buffer.AddOpFunctionCall(function.ResultType ?? -1, function.ResultId ?? -1, stackalloc IdRef[] {
            var1,
            var2,
            var3,
            var4,
            var5,
            var6
        });
        return this;
    }
    public FunctionBuilder CallFunction(
        string functionName,
        Instruction param1,
        Instruction param2,
        Instruction param3,
        Instruction param4,
        Instruction param5,
        Instruction param6,
        Instruction param7
    )
    {
        var var1 = mixer.Buffer.AddOpVariable(mixer.FindType(param1), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param1, null);
        var var2 = mixer.Buffer.AddOpVariable(mixer.FindType(param2), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param2, null);
        var var3 = mixer.Buffer.AddOpVariable(mixer.FindType(param3), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param3, null);
        var var4 = mixer.Buffer.AddOpVariable(mixer.FindType(param4), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param4, null);
        var var5 = mixer.Buffer.AddOpVariable(mixer.FindType(param5), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param5, null);
        var var6 = mixer.Buffer.AddOpVariable(mixer.FindType(param6), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param6, null);
        var var7 = mixer.Buffer.AddOpVariable(mixer.FindType(param7), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param7, null);

        var function = mixer.Buffer.Functions[functionName][0];
        mixer.Buffer.AddOpFunctionCall(function.ResultType ?? -1, function.ResultId ?? -1, stackalloc IdRef[] {
            var1,
            var2,
            var3,
            var4,
            var5,
            var6,
            var7
        });
        return this;
    }
    public FunctionBuilder CallFunction(
        string functionName,
        Instruction param1,
        Instruction param2,
        Instruction param3,
        Instruction param4,
        Instruction param5,
        Instruction param6,
        Instruction param7,
        Instruction param8
    )
    {
        var var1 = mixer.Buffer.AddOpVariable(mixer.FindType(param1), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param1, null);
        var var2 = mixer.Buffer.AddOpVariable(mixer.FindType(param2), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param2, null);
        var var3 = mixer.Buffer.AddOpVariable(mixer.FindType(param3), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param3, null);
        var var4 = mixer.Buffer.AddOpVariable(mixer.FindType(param4), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param4, null);
        var var5 = mixer.Buffer.AddOpVariable(mixer.FindType(param5), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param5, null);
        var var6 = mixer.Buffer.AddOpVariable(mixer.FindType(param6), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param6, null);
        var var7 = mixer.Buffer.AddOpVariable(mixer.FindType(param7), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param7, null);
        var var8 = mixer.Buffer.AddOpVariable(mixer.FindType(param8), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param8, null);

        var function = mixer.Buffer.Functions[functionName][0];
        mixer.Buffer.AddOpFunctionCall(function.ResultType ?? -1, function.ResultId ?? -1, stackalloc IdRef[] {
            var1,
            var2,
            var3,
            var4,
            var5,
            var6,
            var7,
            var8
        });
        return this;
    }
    public FunctionBuilder CallFunction(
        string functionName,
        Instruction param1,
        Instruction param2,
        Instruction param3,
        Instruction param4,
        Instruction param5,
        Instruction param6,
        Instruction param7,
        Instruction param8,
        Instruction param9
    )
    {
        var var1 = mixer.Buffer.AddOpVariable(mixer.FindType(param1), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param1, null);
        var var2 = mixer.Buffer.AddOpVariable(mixer.FindType(param2), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param2, null);
        var var3 = mixer.Buffer.AddOpVariable(mixer.FindType(param3), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param3, null);
        var var4 = mixer.Buffer.AddOpVariable(mixer.FindType(param4), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param4, null);
        var var5 = mixer.Buffer.AddOpVariable(mixer.FindType(param5), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param5, null);
        var var6 = mixer.Buffer.AddOpVariable(mixer.FindType(param6), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param6, null);
        var var7 = mixer.Buffer.AddOpVariable(mixer.FindType(param7), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param7, null);
        var var8 = mixer.Buffer.AddOpVariable(mixer.FindType(param8), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param8, null);
        var var9 = mixer.Buffer.AddOpVariable(mixer.FindType(param9), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param9, null);

        var function = mixer.Buffer.Functions[functionName][0];
        mixer.Buffer.AddOpFunctionCall(function.ResultType ?? -1, function.ResultId ?? -1, stackalloc IdRef[] {
            var1,
            var2,
            var3,
            var4,
            var5,
            var6,
            var7,
            var8,
            var9
        });
        return this;
    }
    public FunctionBuilder CallFunction(
        string functionName,
        Instruction param1,
        Instruction param2,
        Instruction param3,
        Instruction param4,
        Instruction param5,
        Instruction param6,
        Instruction param7,
        Instruction param8,
        Instruction param9,
        Instruction param10
    )
    {
        var var1 = mixer.Buffer.AddOpVariable(mixer.FindType(param1), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param1, null);
        var var2 = mixer.Buffer.AddOpVariable(mixer.FindType(param2), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param2, null);
        var var3 = mixer.Buffer.AddOpVariable(mixer.FindType(param3), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param3, null);
        var var4 = mixer.Buffer.AddOpVariable(mixer.FindType(param4), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param4, null);
        var var5 = mixer.Buffer.AddOpVariable(mixer.FindType(param5), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param5, null);
        var var6 = mixer.Buffer.AddOpVariable(mixer.FindType(param6), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param6, null);
        var var7 = mixer.Buffer.AddOpVariable(mixer.FindType(param7), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param7, null);
        var var8 = mixer.Buffer.AddOpVariable(mixer.FindType(param8), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param8, null);
        var var9 = mixer.Buffer.AddOpVariable(mixer.FindType(param9), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param9, null);
        var var10 = mixer.Buffer.AddOpVariable(mixer.FindType(param10), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param10, null);

        var function = mixer.Buffer.Functions[functionName][0];
        mixer.Buffer.AddOpFunctionCall(function.ResultType ?? -1, function.ResultId ?? -1, stackalloc IdRef[] {
            var1,
            var2,
            var3,
            var4,
            var5,
            var6,
            var7,
            var8,
            var9,
            var10
        });
        return this;
    }
    public FunctionBuilder CallFunction(
        string functionName,
        Instruction param1,
        Instruction param2,
        Instruction param3,
        Instruction param4,
        Instruction param5,
        Instruction param6,
        Instruction param7,
        Instruction param8,
        Instruction param9,
        Instruction param10,
        Instruction param11
    )
    {
        var var1 = mixer.Buffer.AddOpVariable(mixer.FindType(param1), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param1, null);
        var var2 = mixer.Buffer.AddOpVariable(mixer.FindType(param2), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param2, null);
        var var3 = mixer.Buffer.AddOpVariable(mixer.FindType(param3), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param3, null);
        var var4 = mixer.Buffer.AddOpVariable(mixer.FindType(param4), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param4, null);
        var var5 = mixer.Buffer.AddOpVariable(mixer.FindType(param5), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param5, null);
        var var6 = mixer.Buffer.AddOpVariable(mixer.FindType(param6), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param6, null);
        var var7 = mixer.Buffer.AddOpVariable(mixer.FindType(param7), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param7, null);
        var var8 = mixer.Buffer.AddOpVariable(mixer.FindType(param8), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param8, null);
        var var9 = mixer.Buffer.AddOpVariable(mixer.FindType(param9), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param9, null);
        var var10 = mixer.Buffer.AddOpVariable(mixer.FindType(param10), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param10, null);
        var var11 = mixer.Buffer.AddOpVariable(mixer.FindType(param11), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param11, null);

        var function = mixer.Buffer.Functions[functionName][0];
        mixer.Buffer.AddOpFunctionCall(function.ResultType ?? -1, function.ResultId ?? -1, stackalloc IdRef[] {
            var1,
            var2,
            var3,
            var4,
            var5,
            var6,
            var7,
            var8,
            var9,
            var10,
            var11
        });
        return this;
    }

    public FunctionBuilder CallFunction(
        string functionName,
        Instruction param1,
        Instruction param2,
        Instruction param3,
        Instruction param4,
        Instruction param5,
        Instruction param6,
        Instruction param7,
        Instruction param8,
        Instruction param9,
        Instruction param10,
        Instruction param11,
        Instruction param12
    )
    {
        var var1 = mixer.Buffer.AddOpVariable(mixer.FindType(param1), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param1, null);
        var var2 = mixer.Buffer.AddOpVariable(mixer.FindType(param2), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param2, null);
        var var3 = mixer.Buffer.AddOpVariable(mixer.FindType(param3), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param3, null);
        var var4 = mixer.Buffer.AddOpVariable(mixer.FindType(param4), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param4, null);
        var var5 = mixer.Buffer.AddOpVariable(mixer.FindType(param5), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param5, null);
        var var6 = mixer.Buffer.AddOpVariable(mixer.FindType(param6), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param6, null);
        var var7 = mixer.Buffer.AddOpVariable(mixer.FindType(param7), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param7, null);
        var var8 = mixer.Buffer.AddOpVariable(mixer.FindType(param8), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param8, null);
        var var9 = mixer.Buffer.AddOpVariable(mixer.FindType(param9), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param9, null);
        var var10 = mixer.Buffer.AddOpVariable(mixer.FindType(param10), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param10, null);
        var var11 = mixer.Buffer.AddOpVariable(mixer.FindType(param11), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param11, null);
        var var12 = mixer.Buffer.AddOpVariable(mixer.FindType(param12), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param12, null);

        var function = mixer.Buffer.Functions[functionName][0];
        mixer.Buffer.AddOpFunctionCall(function.ResultType ?? -1, function.ResultId ?? -1, stackalloc IdRef[] {
            var1,
            var2,
            var3,
            var4,
            var5,
            var6,
            var7,
            var8,
            var9,
            var10,
            var11,
            var12
        });
        return this;
    }
    public FunctionBuilder CallFunction(
        string functionName,
        Instruction param1,
        Instruction param2,
        Instruction param3,
        Instruction param4,
        Instruction param5,
        Instruction param6,
        Instruction param7,
        Instruction param8,
        Instruction param9,
        Instruction param10,
        Instruction param11,
        Instruction param12,
        Instruction param13
    )
    {
        var var1 = mixer.Buffer.AddOpVariable(mixer.FindType(param1), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param1, null);
        var var2 = mixer.Buffer.AddOpVariable(mixer.FindType(param2), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param2, null);
        var var3 = mixer.Buffer.AddOpVariable(mixer.FindType(param3), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param3, null);
        var var4 = mixer.Buffer.AddOpVariable(mixer.FindType(param4), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param4, null);
        var var5 = mixer.Buffer.AddOpVariable(mixer.FindType(param5), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param5, null);
        var var6 = mixer.Buffer.AddOpVariable(mixer.FindType(param6), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param6, null);
        var var7 = mixer.Buffer.AddOpVariable(mixer.FindType(param7), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param7, null);
        var var8 = mixer.Buffer.AddOpVariable(mixer.FindType(param8), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param8, null);
        var var9 = mixer.Buffer.AddOpVariable(mixer.FindType(param9), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param9, null);
        var var10 = mixer.Buffer.AddOpVariable(mixer.FindType(param10), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param10, null);
        var var11 = mixer.Buffer.AddOpVariable(mixer.FindType(param11), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param11, null);
        var var12 = mixer.Buffer.AddOpVariable(mixer.FindType(param12), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param12, null);
        var var13 = mixer.Buffer.AddOpVariable(mixer.FindType(param13), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param13, null);

        var function = mixer.Buffer.Functions[functionName][0];
        mixer.Buffer.AddOpFunctionCall(function.ResultType ?? -1, function.ResultId ?? -1, stackalloc IdRef[] {
            var1,
            var2,
            var3,
            var4,
            var5,
            var6,
            var7,
            var8,
            var9,
            var10,
            var11,
            var12,
            var13,
        });
        return this;
    }
    public FunctionBuilder CallFunction(
        string functionName,
        Instruction param1,
        Instruction param2,
        Instruction param3,
        Instruction param4,
        Instruction param5,
        Instruction param6,
        Instruction param7,
        Instruction param8,
        Instruction param9,
        Instruction param10,
        Instruction param11,
        Instruction param12,
        Instruction param13,
        Instruction param14
    )
    {
        var var1 = mixer.Buffer.AddOpVariable(mixer.FindType(param1), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param1, null);
        var var2 = mixer.Buffer.AddOpVariable(mixer.FindType(param2), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param2, null);
        var var3 = mixer.Buffer.AddOpVariable(mixer.FindType(param3), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param3, null);
        var var4 = mixer.Buffer.AddOpVariable(mixer.FindType(param4), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param4, null);
        var var5 = mixer.Buffer.AddOpVariable(mixer.FindType(param5), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param5, null);
        var var6 = mixer.Buffer.AddOpVariable(mixer.FindType(param6), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param6, null);
        var var7 = mixer.Buffer.AddOpVariable(mixer.FindType(param7), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param7, null);
        var var8 = mixer.Buffer.AddOpVariable(mixer.FindType(param8), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param8, null);
        var var9 = mixer.Buffer.AddOpVariable(mixer.FindType(param9), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param9, null);
        var var10 = mixer.Buffer.AddOpVariable(mixer.FindType(param10), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param10, null);
        var var11 = mixer.Buffer.AddOpVariable(mixer.FindType(param11), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param11, null);
        var var12 = mixer.Buffer.AddOpVariable(mixer.FindType(param12), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param12, null);
        var var13 = mixer.Buffer.AddOpVariable(mixer.FindType(param13), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param13, null);
        var var14 = mixer.Buffer.AddOpVariable(mixer.FindType(param14), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param14, null);


        var function = mixer.Buffer.Functions[functionName][0];
        mixer.Buffer.AddOpFunctionCall(function.ResultType ?? -1, function.ResultId ?? -1, stackalloc IdRef[] {
            var1,
            var2,
            var3,
            var4,
            var5,
            var6,
            var7,
            var8,
            var9,
            var10,
            var11,
            var12,
            var13,
            var14,
        });
        return this;
    }
    public FunctionBuilder CallFunction(
        string functionName,
        Instruction param1,
        Instruction param2,
        Instruction param3,
        Instruction param4,
        Instruction param5,
        Instruction param6,
        Instruction param7,
        Instruction param8,
        Instruction param9,
        Instruction param10,
        Instruction param11,
        Instruction param12,
        Instruction param13,
        Instruction param14,
        Instruction param15
    )
    {
        var var1 = mixer.Buffer.AddOpVariable(mixer.FindType(param1), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param1, null);
        var var2 = mixer.Buffer.AddOpVariable(mixer.FindType(param2), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param2, null);
        var var3 = mixer.Buffer.AddOpVariable(mixer.FindType(param3), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param3, null);
        var var4 = mixer.Buffer.AddOpVariable(mixer.FindType(param4), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param4, null);
        var var5 = mixer.Buffer.AddOpVariable(mixer.FindType(param5), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param5, null);
        var var6 = mixer.Buffer.AddOpVariable(mixer.FindType(param6), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param6, null);
        var var7 = mixer.Buffer.AddOpVariable(mixer.FindType(param7), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param7, null);
        var var8 = mixer.Buffer.AddOpVariable(mixer.FindType(param8), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param8, null);
        var var9 = mixer.Buffer.AddOpVariable(mixer.FindType(param9), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param9, null);
        var var10 = mixer.Buffer.AddOpVariable(mixer.FindType(param10), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param10, null);
        var var11 = mixer.Buffer.AddOpVariable(mixer.FindType(param11), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param11, null);
        var var12 = mixer.Buffer.AddOpVariable(mixer.FindType(param12), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param12, null);
        var var13 = mixer.Buffer.AddOpVariable(mixer.FindType(param13), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param13, null);
        var var14 = mixer.Buffer.AddOpVariable(mixer.FindType(param14), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param14, null);
        var var15 = mixer.Buffer.AddOpVariable(mixer.FindType(param15), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param15, null);
        var function = mixer.Buffer.Functions[functionName][0];
        mixer.Buffer.AddOpFunctionCall(function.ResultType ?? -1, function.ResultId ?? -1, stackalloc IdRef[] {
            var1,
            var2,
            var3,
            var4,
            var5,
            var6,
            var7,
            var8,
            var9,
            var10,
            var11,
            var12,
            var13,
            var14,
            var15,
        });
        return this;
    }
    public FunctionBuilder CallFunction(
        string functionName,
        Instruction param1,
        Instruction param2,
        Instruction param3,
        Instruction param4,
        Instruction param5,
        Instruction param6,
        Instruction param7,
        Instruction param8,
        Instruction param9,
        Instruction param10,
        Instruction param11,
        Instruction param12,
        Instruction param13,
        Instruction param14,
        Instruction param15,
        Instruction param16
    )
    {
        var var1 = mixer.Buffer.AddOpVariable(mixer.FindType(param1), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param1, null);
        var var2 = mixer.Buffer.AddOpVariable(mixer.FindType(param2), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param2, null);
        var var3 = mixer.Buffer.AddOpVariable(mixer.FindType(param3), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param3, null);
        var var4 = mixer.Buffer.AddOpVariable(mixer.FindType(param4), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param4, null);
        var var5 = mixer.Buffer.AddOpVariable(mixer.FindType(param5), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param5, null);
        var var6 = mixer.Buffer.AddOpVariable(mixer.FindType(param6), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param6, null);
        var var7 = mixer.Buffer.AddOpVariable(mixer.FindType(param7), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param7, null);
        var var8 = mixer.Buffer.AddOpVariable(mixer.FindType(param8), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param8, null);
        var var9 = mixer.Buffer.AddOpVariable(mixer.FindType(param9), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param9, null);
        var var10 = mixer.Buffer.AddOpVariable(mixer.FindType(param10), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param10, null);
        var var11 = mixer.Buffer.AddOpVariable(mixer.FindType(param11), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param11, null);
        var var12 = mixer.Buffer.AddOpVariable(mixer.FindType(param12), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param12, null);
        var var13 = mixer.Buffer.AddOpVariable(mixer.FindType(param13), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param13, null);
        var var14 = mixer.Buffer.AddOpVariable(mixer.FindType(param14), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param14, null);
        var var15 = mixer.Buffer.AddOpVariable(mixer.FindType(param15), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param15, null);
        var var16 = mixer.Buffer.AddOpVariable(mixer.FindType(param16), StorageClass.Function, null);
        mixer.Buffer.AddOpStore(var1, param16, null);


        var function = mixer.Buffer.Functions[functionName][0];
        mixer.Buffer.AddOpFunctionCall(function.ResultType ?? -1, function.ResultId ?? -1, stackalloc IdRef[] {
            var1,
            var2,
            var3,
            var4,
            var5,
            var6,
            var7,
            var8,
            var9,
            var10,
            var11,
            var12,
            var13,
            var14,
            var15,
            var16,
        });
        return this;
    }

}

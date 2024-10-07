using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using static Stride.Shaders.Spirv.Mixer;
using static Spv.Specification;

namespace Stride.Shaders.Spirv;


public ref partial struct FunctionBuilder
{
    public record struct Variable(IdRef Id, bool IsVariable);
    public record struct Value(IdRef Id, IdResultType Type, bool IsVariable = false)
    {
        public static implicit operator Value(Instruction i) => new(i, i.ResultType ?? -1, false);
    };
    public delegate Variable InitializerDelegate(Mixer mixer, ref FunctionBuilder functionBuilder);
    public delegate Value ValueDelegate(Mixer mixer, FunctionBuilder functionBuilder);

    internal Mixer mixer;
    Instruction function;
    EntryPoint? entryPoint;

    public FunctionBuilder(Mixer mixer, string returnType, string name, CreateFunctionParameters parameterTypesDelegate)
    {
        this.mixer = mixer;
        

        var t = mixer.GetOrCreateBaseType(returnType.AsMemory());
        function = mixer.Buffer.AddOpSDSLFunction(t.ResultId ?? -1, FunctionControlMask.MaskNone, -1, name);
        var p = new ParameterBuilder(mixer, stackalloc IdRef[16], stackalloc IdRef[16]);
        p = parameterTypesDelegate.Invoke(p);
        var t_func = mixer.Buffer.AddOpTypeFunction(t.ResultId ?? -1, p.Types);
        function.Operands.Span[3] = t_func.ResultId ?? -1;
        mixer.Buffer.AddOpLabel();
    }
    public FunctionBuilder(Mixer mixer, EntryPoint entryPoint)
    {
        this.mixer = mixer;
        this.entryPoint = entryPoint;
        var t = mixer.GetOrCreateBaseType("void".AsMemory());
        var t_func = mixer.Buffer.AddOpTypeFunction(t.ResultId ?? -1, Span<IdRef>.Empty);
        function = mixer.Buffer.AddOpSDSLFunction(t.ResultId ?? -1, FunctionControlMask.MaskNone, t_func, entryPoint.Name);
        mixer.Buffer.AddOpLabel();

    }



    public FunctionBuilder Declare(string type, string name)
    {
        var t = mixer.GetOrCreateBaseType(type.AsMemory());
        var p_t = mixer.Buffer.AddOpTypePointer(StorageClass.Function, t);
        mixer.Buffer.AddOpSDSLVariable(p_t, StorageClass.Function, name, null);
        return this;
    }
    /// <summary>
    /// Declares a variable and assigns a value to it
    /// </summary>
    /// <param name="type"></param>
    /// <param name="name"></param>
    /// <param name="initializer"></param>
    /// <returns></returns>
    public FunctionBuilder DeclareAssign<T>(string name, T constant)
        where T : struct
    {
        var resultType = mixer.GetOrCreateBaseType<T>();
        var ptr = mixer.Buffer.AddOpTypePointer(StorageClass.Function, resultType.ResultId ?? -1);
        var value = mixer.CreateConstant(constant);
        mixer.Buffer.AddOpSDSLVariable(ptr, StorageClass.Function, name, value.ResultId);
        return this;
    }
    public FunctionBuilder Assign(string name, ValueDelegate initializer)
    {
        // TODO : If the value delegate is a constant no need to be loaded on a register
        var result = initializer.Invoke(mixer, this);
        if (mixer.LocalVariables.TryGet(name, out var local))
        {
            var load = result.Id;
            if(result.IsVariable)
                load = mixer.Buffer.AddOpLoad(result.Type, result.Id, null).ResultId ?? -1;
            mixer.Buffer.AddOpStore(local, load, null);
        }
        else if (mixer.GlobalVariables.TryGet(name, out var global))
        {
            var load = result.Id;
            if (result.IsVariable)
                load = mixer.Buffer.AddOpLoad(result.Type, result.Id, null);
            mixer.Buffer.AddOpStore(global, load, null);
        }
        return this;

    }
    public FunctionBuilder Assign(string name, IdRef value)
    {
        if (mixer.LocalVariables.TryGet(name, out var local))
            mixer.Buffer.AddOpStore(local, value, null);
        else if (mixer.GlobalVariables.TryGet(name, out var global))
            mixer.Buffer.AddOpStore(global, value, null);
        return this;
    }
    public FunctionBuilder AssignConstant<T>(string name, T constantValue)
        where T : struct
    {
        var constant = mixer.CreateConstant(constantValue);
        if (mixer.LocalVariables.TryGet(name, out var local))
            mixer.Buffer.AddOpStore(local, constant, null);
        else if (mixer.GlobalVariables.TryGet(name, out var global))
            mixer.Buffer.AddOpStore(global, constant, null);
        return this;
    }
    public FunctionBuilder AssignVariable(string destination, string source)
    {
        // TODO : If the value delegate is a constant no need to be loaded on a register

        Instruction src = Instruction.Empty;
        if (mixer.LocalVariables.TryGet(source, out var ls))
            src = ls;
        else if (mixer.GlobalVariables.TryGet(source, out var gs))
            src = gs;

        var srcType = mixer.FindType(src.Words.Span[1]);

        if (mixer.LocalVariables.TryGet(destination, out var local))
        {
            var load = mixer.Buffer.AddOpLoad(srcType, src, null);
            mixer.Buffer.AddOpStore(local, load, null);
        }
        else if (mixer.GlobalVariables.TryGet(destination, out var global))
        {
            var load = mixer.Buffer.AddOpLoad(srcType, src, null);
            mixer.Buffer.AddOpStore(global, load, null);
        }
        return this;
    }
    public FunctionBuilder Return(ValueDelegate vd)
    {
        Return(vd.Invoke(mixer, this).Id);
        return this;
    }

    public FunctionBuilder Return(IdRef? value = null)
    {
        if (value != null)
            mixer.Buffer.AddOpReturnValue(value.Value);
        else
            mixer.Buffer.AddOpReturn();
        return this;
    }
    public Mixer FunctionEnd()
    {
        var count = mixer.IOVariables.Count;
        Span<IdRef> idRefs = stackalloc IdRef[count];
        int index = 0;
        foreach (var i in mixer.IOVariables)
        {
            idRefs[index] = i;
            index += 1;
        }
        mixer.Buffer.AddOpFunctionEnd();
        if (entryPoint != null)
        {
            mixer.Buffer.AddOpEntryPoint(entryPoint.Value.ExecutionModel, function, entryPoint.Value.Name, idRefs);
        }
        return mixer;
    }

    public delegate ParameterBuilder CreateFunctionParameters(ParameterBuilder typeIds);
    public ref struct ParameterBuilder
    {
        Mixer mixer;
        Span<IdRef> inner { get; }
        Span<IdRef> innerTypes { get; }
        public Span<IdRef> Parameters => inner[..count];
        public Span<IdRef> Types => innerTypes[..count];
        int count;
        public ParameterBuilder(Mixer mixer, Span<IdRef> parameters, Span<IdRef> types)
        {
            this.mixer = mixer;
            parameters.Clear();
            inner = parameters;
            innerTypes = types;
            count = 0;
        }

        public ParameterBuilder With(string type, string name)
        {
            var i = mixer.Buffer.AddOpSDSLFunctionParameter(mixer.GetOrCreateBaseType(type.AsMemory()), name);
            inner[count] = i.ResultId ?? -1;
            innerTypes[count] = i.ResultType ?? -1;
            count += 1;
            return this;
        }
    }
}

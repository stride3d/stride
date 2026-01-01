using CommunityToolkit.HighPerformance;
using Stride.Shaders.Core;
using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Processing;
using Stride.Shaders.Spirv.Tools;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Building;

public interface IExternalShaderLoader
{
    public void RegisterShader(string name, ReadOnlySpan<ShaderMacro> defines, SpirvBytecode buffer);
    public bool Exists(string name);
    public bool LoadExternalBuffer(string name, ReadOnlySpan<ShaderMacro> defines, [MaybeNullWhen(false)] out SpirvBytecode bytecode, out bool isFromCache);
    public bool LoadExternalBuffer(string name, string code, ReadOnlySpan<ShaderMacro> defines, [MaybeNullWhen(false)] out SpirvBytecode bytecode, out bool isFromCache);
}

// Should contain internal data not seen by the client but helpful for the generation like type symbols and other 
// SPIR-V parameters
public partial class SpirvContext
{
    private int bound = 1;
    public int ResourceGroupBound { get; set; } = 1;
    public ref int Bound => ref bound;
    public Dictionary<SymbolType, int> Types { get; init; } = [];
    public Dictionary<int, SymbolType> ReverseTypes { get; init; } = [];
    public Dictionary<int, string> Names { get; init; } = [];
    
    public OpDataIndex this[int index] => new(index, Buffer);

    public int Count => Buffer.Count;

    NewSpirvBuffer Buffer { get; init; }

    public int? GLSLSet { get; private set; }

    public SpirvContext()
    {
        Buffer = new();
    }

    public SpirvContext(NewSpirvBuffer buffer)
    {
        Buffer = buffer;
    }

    public void ImportGLSL()
    {
        foreach(var i in Buffer)
        {
            if(i.Op == Op.OpExtInstImport && (OpExtInstImport)i is { Name: "GLSL.std.450" })
            {
                GLSLSet ??= ((OpExtInstImport)i).ResultId;
                return;
            }
        }
        Buffer.Insert(1, new OpExtInstImport(Bound++, "GLSL.std.450"));
        GLSLSet = Bound - 1;
    }

    public void AddName(int target, string name)
    {
        Buffer.Add(new OpName(target, name));
        Names.Add(target, name);
    }

    public void AddMemberName(int target, int accessor, string name)
        => Buffer.Add(new OpMemberName(target, accessor, name.Replace('.', '_')));

    public void SetEntryPoint(ExecutionModel model, int function, string name, ReadOnlySpan<Symbol> variables)
    {
        Span<int> pvariables = stackalloc int[variables.Length];
        int pos = 0;
        foreach (var v in variables)
            pvariables[pos++] = v.IdRef;
        Buffer.Add(new OpEntryPoint(model, function, name, [.. pvariables]));
    }


    public T Insert<T>(int index, in T value)
        where T : struct, IMemoryInstruction, allows ref struct
        => Buffer.Insert(index, value);

    public OpData InsertData<T>(int index, in T value)
        where T : struct, IMemoryInstruction, allows ref struct
        => Buffer.InsertData(index, value);

    public OpDataIndex Insert(int index, OpData data)
        => Buffer.Insert(index, data);

    public OpData Add<T>(in T value)
        where T : struct, IMemoryInstruction, allows ref struct
        => Buffer.Add(value);

    public OpDataIndex Add(OpData data)
        => Buffer.Add(data);

    public void RemoveAt(int index, bool dispose = true)
        => Buffer.RemoveAt(index, dispose);

    public OpData Replace<T>(int index, in T instruction) where T : struct, IMemoryInstruction, allows ref struct
        => Buffer.Replace(index, instruction);

    public SpirvContext FluentAdd<T>(in T value, out T result)
        where T : struct, IMemoryInstruction, allows ref struct
    {
        Buffer.FluentAdd(value, out result);
        return this;
    }

    public void Sort() => Buffer.Sort();

    [Obsolete("Use the insert method instead")]
    public NewSpirvBuffer GetBuffer() => Buffer;

    public NewSpirvBuffer.Enumerator GetEnumerator() => Buffer.GetEnumerator();

    public override string ToString()
    {
        return Spv.Dis(Buffer, writeToConsole: false);
    }
}
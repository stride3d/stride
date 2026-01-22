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
using Stride.Core.Storage;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Building;

public interface IShaderCache
{
    public void RegisterShader(string name, ReadOnlySpan<ShaderMacro> defines, ShaderBuffers bytecode, ObjectId? hash);
    public bool Exists(string name);
    public bool TryLoadFromCache(string name, ReadOnlySpan<ShaderMacro> defines, [MaybeNullWhen(false)] out ShaderBuffers buffer, out ObjectId hash);
}

public class ShaderCache : IShaderCache
{
    record struct ShaderLoadKey(ShaderMacro[] Macros)
    {
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = 0;
                foreach (var current in Macros)
                    hashCode = (hashCode * 397) ^ (current.GetHashCode());
                return hashCode;
            }
        }

        public bool Equals(ShaderLoadKey other)
        {
            return Macros.SequenceEqual(other.Macros);
        }
    }

    private Dictionary<string, (ObjectId Hash, Dictionary<ShaderLoadKey, ShaderBuffers> BuffersPerMacros)> loadedShaders = [];

    public bool Exists(string name) => loadedShaders.ContainsKey(name);

    public virtual void RegisterShader(string name, ReadOnlySpan<ShaderMacro> defines, ShaderBuffers bytecode, ObjectId? hash = null)
    {
        ref var loadedShadersByName = ref CollectionsMarshal.GetValueRefOrAddDefault(loadedShaders, name, out var exists);
        if (!exists)
            loadedShadersByName = hash != null ? new(hash.Value, new()) : new();
        loadedShadersByName.BuffersPerMacros.Add(new(defines.ToArray()), bytecode);
        if (hash != null)
            loadedShadersByName.Hash = hash.Value;
    }
    
    public bool TryLoadFromCache(string name, ReadOnlySpan<ShaderMacro> defines, [MaybeNullWhen(false)] out ShaderBuffers buffer, out ObjectId hash)
    {
        if (loadedShaders.TryGetValue(name, out var loadedShadersByName)
            && loadedShadersByName.BuffersPerMacros.TryGetValue(new(defines.ToArray()), out buffer))
        {
            hash = loadedShadersByName.Hash;
            return true;
        }

        hash = default;
        buffer = default;
        return false;
    }
}

public interface IExternalShaderLoader
{
    public IShaderCache FileCache { get; }
    public IShaderCache GenericCache { get; }
    
    public bool Exists(string name);
    public bool LoadExternalFileContent(string name, out string filename, out string code, out ObjectId hash);
    public bool LoadExternalBuffer(string name, ReadOnlySpan<ShaderMacro> defines, [MaybeNullWhen(false)] out ShaderBuffers bytecode, out ObjectId hash, out bool isFromCache);
    public bool LoadExternalBuffer(string name, string code, ReadOnlySpan<ShaderMacro> defines, [MaybeNullWhen(false)] out ShaderBuffers bytecode, out ObjectId hash, out bool isFromCache);
}

// Should contain internal data not seen by the client but helpful for the generation like type symbols and other 
// SPIR-V parameters
public partial class SpirvContext
{
    // Used internally by GenericResolverFromInstantiatingBuffer (cache from constant ID to string representation)
    internal IShaderCache GenericCache { get; } = new ShaderCache();
    internal Dictionary<int, string> GenericValueCache { get; } = new();
    
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

    /// <summary>
    /// Add a new name to a target ID. It should not have been set before.
    /// </summary>
    /// <param name="target"></param>
    /// <param name="name"></param>
    public void AddName(int target, string name)
    {
        Buffer.Add(new OpName(target, name));
        Names.Add(target, name);
    }

    /// <summary>
    /// Adds or updates a name to a target ID.
    /// </summary>
    /// <param name="target"></param>
    /// <param name="name"></param>
    public void SetName(int target, string name)
    {
        Names[target] = name;

        foreach (var i in Buffer)
        {
            if (i.Op == Op.OpName && (OpName)i is { } nameInstruction && nameInstruction.Target == target)
            {
                nameInstruction.Name = name;
                return;
            }
        }
        
        // Not found, create new one
        Buffer.Add(new OpName(target, name));
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

    public void RemoveNameAndDecorations(HashSet<int> ids)
    {
        foreach (var i in  Buffer)
        {
            if (i.Op == Op.OpDecorate && ((OpDecorate)i) is { } decorate)
            {
                if (ids.Contains(decorate.Target))
                    SpirvBuilder.SetOpNop(i.Data.Memory.Span);
            }
            else if (i.Op == Op.OpDecorateString && ((OpDecorateString)i) is { } decorateString)
            {
                if (ids.Contains(decorateString.Target))
                    SpirvBuilder.SetOpNop(i.Data.Memory.Span);
            }
            else if (i.Op == Op.OpName && (OpName)i is {} nameInstruction)
            {
                if (ids.Contains(nameInstruction.Target))
                {
                    Names.Remove(nameInstruction.Target);
                    SpirvBuilder.SetOpNop(i.Data.Memory.Span);
                }
            }
        }
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
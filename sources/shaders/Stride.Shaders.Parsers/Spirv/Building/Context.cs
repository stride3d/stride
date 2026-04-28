using CommunityToolkit.HighPerformance;
using Stride.Shaders.Core;
using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Processing;
using Stride.Shaders.Spirv.Tools;
using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
using Stride.Core.Storage;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Building;

/// <summary>
/// Coordinates parallel generic shader instantiations so only one thread compiles a given (name, generics, macros) combination.
/// </summary>
public class GenericShaderCache
{
    private readonly ConcurrentDictionary<(string Name, string? Generics, int MacrosHash), Lazy<(ShaderBuffers Buffer, ObjectId Hash)>> compilingShaders = new();

    private static int ComputeMacrosHash(ReadOnlySpan<ShaderMacro> macros)
    {
        unchecked
        {
            int hash = 0;
            foreach (var m in macros)
                hash = hash * 397 ^ m.GetHashCode();
            return hash;
        }
    }

    public (ShaderBuffers Buffer, ObjectId Hash) GetOrInstantiate(
        string name, string? generics, ReadOnlySpan<ShaderMacro> macros,
        Func<(ShaderBuffers Buffer, ObjectId Hash)> factory)
    {
        var macrosHash = ComputeMacrosHash(macros);
        var key = (name, generics, macrosHash);

        var lazy = compilingShaders.GetOrAdd(key, _ => new Lazy<(ShaderBuffers, ObjectId)>(factory, LazyThreadSafetyMode.ExecutionAndPublication));

        try
        {
            return lazy.Value;
        }
        catch
        {
            compilingShaders.TryRemove(key, out _);
            throw;
        }
        finally
        {
            compilingShaders.TryRemove(key, out _);
        }
    }
}

public interface IShaderCache
{
    public void RegisterShader(string name, string? generics, ReadOnlySpan<ShaderMacro> defines, ShaderBuffers bytecode, ObjectId? hash);
    public bool Exists(string name);
    public bool TryLoadFromCache(string name, string? generics, ReadOnlySpan<ShaderMacro> defines, [MaybeNullWhen(false)] out ShaderBuffers buffer, out ObjectId hash);
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

    private Dictionary<(string Name, string? Generics), (ObjectId Hash, Dictionary<ShaderLoadKey, ShaderBuffers> BuffersPerMacros)> loadedShaders = [];

    public bool Exists(string name) => loadedShaders.ContainsKey((name, null));

    public virtual void RegisterShader(string name, string? generics, ReadOnlySpan<ShaderMacro> defines, ShaderBuffers bytecode, ObjectId? hash = null)
    {
        bytecode.Context.Frozen = true;

        lock (loadedShaders)
        {
            ref var loadedShadersByName = ref CollectionsMarshal.GetValueRefOrAddDefault(loadedShaders, (name, generics), out var exists);
            if (!exists)
                loadedShadersByName = hash != null ? new(hash.Value, new()) : new();
            loadedShadersByName.BuffersPerMacros[new(defines.ToArray())] = bytecode;
            if (hash != null)
                loadedShadersByName.Hash = hash.Value;
        }
    }

    public bool TryLoadFromCache(string name, string? generics, ReadOnlySpan<ShaderMacro> defines, [MaybeNullWhen(false)] out ShaderBuffers buffer, out ObjectId hash)
    {
        lock (loadedShaders)
        {
            if (loadedShaders.TryGetValue((name, generics), out var loadedShadersByName)
                && loadedShadersByName.BuffersPerMacros.TryGetValue(new(defines.ToArray()), out buffer))
            {
                hash = loadedShadersByName.Hash;
                return true;
            }
        }

        hash = default;
        buffer = default;
        return false;
    }
}

public interface IExternalShaderLoader
{
    public IShaderCache Cache { get; }
    public GenericShaderCache GenericCache { get; }

    public bool Exists(string name);
    public bool LoadExternalFileContent(string name, out string filename, out string code, out ObjectId hash);
    public bool LoadExternalBuffer(string name, ReadOnlySpan<ShaderMacro> defines, [MaybeNullWhen(false)] out ShaderBuffers bytecode, out ObjectId hash, out bool isFromCache);
    public bool LoadExternalBuffer(string name, string? filename, string code, ReadOnlySpan<ShaderMacro> defines, [MaybeNullWhen(false)] out ShaderBuffers bytecode, out ObjectId hash, out bool isFromCache);

    /// <summary>When set to true, suppresses OpSourceHashSDSL emission for the next compilation (used by MemberName recompilations).</summary>
    bool SuppressSourceHash { get; set; }
}

// Should contain internal data not seen by the client but helpful for the generation like type symbols and other 
// SPIR-V parameters
public partial class SpirvContext
{
    // Used internally by GenericResolverFromInstantiatingBuffer
    internal IShaderCache GenericCache { get; } = new ShaderCache();

    private int bound = 1;
    public int ResourceGroupBound { get; set; } = 1;
    public ref int Bound => ref bound;
    public FreezeableDictionary<SymbolType, int> Types { get; init; } = new();
    public FreezeableDictionary<int, SymbolType> ReverseTypes { get; init; } = new();
    public FreezeableDictionary<int, string> Names { get; init; } = new();

    /// <summary>
    /// When true, any mutation to this context will throw.
    /// Set after caching to catch thread-safety violations during development.
    /// </summary>
    public bool Frozen
    {
        get => frozen;
        set
        {
            frozen = value;
            Types.Frozen = value;
            ReverseTypes.Frozen = value;
            Names.Frozen = value;
        }
    }
    private bool frozen;

    private void ThrowIfFrozen()
    {
        if (Frozen)
            throw new InvalidOperationException("Attempted to mutate a frozen SpirvContext. Cached shader contexts must not be modified.");
    }

    public OpDataIndex this[int index] => new(index, Buffer);

    public int Count => Buffer.Count;

    SpirvBuffer Buffer { get; init; }

    public int? GLSLSet { get; private set; }

    public SpirvContext()
    {
        Buffer = new();
    }

    public SpirvContext(SpirvBuffer buffer)
    {
        Buffer = buffer;
    }

    public void ImportGLSL()
    {
        foreach (var i in Buffer)
        {
            if (i.Op == Op.OpExtInstImport && (OpExtInstImport)i is { Name: "GLSL.std.450" })
            {
                GLSLSet ??= ((OpExtInstImport)i).ResultId;
                return;
            }
        }
        Buffer.Insert(1, new OpExtInstImport(Bound++, "GLSL.std.450"));
        GLSLSet = Bound - 1;
    }

    public int GetGLSL()
    {
        if (GLSLSet == null)
            ImportGLSL();

        return GLSLSet!.Value;
    }

    /// <summary>
    /// Add a new name to a target ID. It should not have been set before.
    /// </summary>
    /// <param name="target"></param>
    /// <param name="name"></param>
    public void AddName(int target, string name)
    {
        ThrowIfFrozen();
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
        ThrowIfFrozen();
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
    {
        ThrowIfFrozen();
        Buffer.AddData(new OpMemberName(target, accessor, name.Replace('.', '_')));
    }

    public void SetEntryPoint(ExecutionModel model, int function, string name, ReadOnlySpan<Symbol> variables)
    {
        ThrowIfFrozen();
        Span<int> pvariables = stackalloc int[variables.Length];
        int pos = 0;
        foreach (var v in variables)
            pvariables[pos++] = v.IdRef;
        Buffer.Add(new OpEntryPoint(model, function, name, [.. pvariables]));
    }


    public T Insert<T>(int index, in T value)
        where T : struct, IMemoryInstruction, allows ref struct
    { ThrowIfFrozen(); return Buffer.Insert(index, value); }

    public OpData InsertData<T>(int index, in T value)
        where T : struct, IMemoryInstruction, allows ref struct
    { ThrowIfFrozen(); return Buffer.InsertData(index, value); }

    public OpDataIndex Insert(int index, OpData data)
    { ThrowIfFrozen(); return Buffer.Insert(index, data); }

    public T Add<T>(in T value)
        where T : struct, IMemoryInstruction, allows ref struct
    { ThrowIfFrozen(); return Buffer.Add(value); }

    public OpData AddData<T>(in T value)
        where T : struct, IMemoryInstruction, allows ref struct
    { ThrowIfFrozen(); return Buffer.AddData(value); }

    public OpDataIndex Add(OpData data)
    { ThrowIfFrozen(); return Buffer.Add(data); }

    public void RemoveAt(int index, bool dispose = true)
    { ThrowIfFrozen(); Buffer.RemoveAt(index, dispose); }

    public OpData Replace<T>(int index, in T instruction) where T : struct, IMemoryInstruction, allows ref struct
    { ThrowIfFrozen(); return Buffer.Replace(index, instruction); }

    public SpirvContext FluentAdd<T>(in T value, out T result)
        where T : struct, IMemoryInstruction, allows ref struct
    {
        ThrowIfFrozen();
        Buffer.FluentAdd(value, out result);
        return this;
    }

    public void RemoveNameAndDecorations(HashSet<int> ids)
    {
        ThrowIfFrozen();
        foreach (var i in Buffer)
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
            else if (i.Op == Op.OpName && (OpName)i is { } nameInstruction)
            {
                if (ids.Contains(nameInstruction.Target))
                {
                    Names.Remove(nameInstruction.Target);
                    SpirvBuilder.SetOpNop(i.Data.Memory.Span);
                }
            }
        }
    }

    public void Sort() { ThrowIfFrozen(); Buffer.Sort(); }

    public SpirvBuffer GetBuffer() => Buffer;

    public SpirvBuffer.Enumerator GetEnumerator() => Buffer.GetEnumerator();

    public override string ToString()
    {
        return Spv.Dis(Buffer, writeToConsole: false);
    }
}

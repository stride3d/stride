using System.Runtime.InteropServices;
using Stride.Shaders.Core;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Core;

namespace Stride.Shaders.Spirv.Building;

public partial class SpirvContext
{
    // Cache for RuntimeArray types keyed by element type ID, to avoid creating duplicates
    // that would lose their ArrayStride decoration during type deduplication in the mixer.
    private Dictionary<int, int> runtimeArrayCache = [];

    // Maps logical shader class name (e.g. "SomeMixin<3>") to the registered import ID,
    // to deduplicate ShaderDefinitions that represent the same logical shader but differ
    // only in their GenericArguments int[] reference (which breaks record equality).
    private Dictionary<string, int> shaderImportIds = [];

    // Cache of known ShaderDefinitions by name, populated by GetOrImportShader.
    // Used by GetOrRegister to resolve ShaderSymbol → full import when encountering
    // shader types from cached dependency contexts.
    private Dictionary<string, ShaderDefinition> knownShaderDefs = [];

    private int GetOrCreateRuntimeArray(int elementTypeId, int arrayStride)
    {
        if (runtimeArrayCache.TryGetValue(elementTypeId, out var id))
            return id;

        id = Buffer.Add(new OpTypeRuntimeArray(Bound++, elementTypeId)).ResultId;
        Buffer.Add(new OpDecorate(id, Specification.Decoration.ArrayStride, [arrayStride]));
        runtimeArrayCache[elementTypeId] = id;
        return id;
    }

    public int GetOrRegister(SymbolType? type)
    {
        if (type is null)
            throw new ArgumentException($"Type is null");
        if (Types.TryGetValue(type, out var res))
            return res;
        // ShaderSymbol is a lightweight placeholder — its ID is tracked in shaderImportIds, not Types
        if (type is ShaderSymbol ss)
        {
            if (shaderImportIds.TryGetValue(ss.Name, out var importId))
                return importId;
            // Try full import if the ShaderDefinition is known (populated by GetOrImportShader)
            if (knownShaderDefs.TryGetValue(ss.Name, out var shaderDef))
                return GetOrImportShader(shaderDef);
            // Fallback: create a minimal import for unresolved shader symbols
            ThrowIfFrozen();
            var id = Bound++;
            var emittedArgs = new int[ss.GenericArguments.Length];
            for (int i = 0; i < emittedArgs.Length; i++)
                emittedArgs[i] = ss.GenericArguments[i].Emit(this);
            Add(new OpImportShaderSDSL(id, new(ss.Name), new(emittedArgs.AsSpan())));
            AddName(id, ss.Name);
            shaderImportIds[ss.Name] = id;
            return id;
        }

        ThrowIfFrozen();
        return RegisterType(type, Bound++);
    }

    /// <summary>
    /// Returns the SPIR-V import ID for a shader definition, creating an OpImportShaderSDSL
    /// instruction if this is the first time this shader is imported in this context.
    /// Struct types from the shader are registered in Types/ReverseTypes (they are real SPIR-V types).
    /// </summary>
    public int GetOrImportShader(ShaderDefinition shaderDef)
    {
        var key = shaderDef.Name; // For non-generic shaders
        if (shaderDef.GenericArguments.Length > 0)
        {
            var resolved = ResolveShaderStringKey(shaderDef.Name, shaderDef.GenericArguments);
            if (resolved != null)
                key = resolved;
        }

        // Cache the definition so GetOrRegister can resolve ShaderSymbol → full import
        knownShaderDefs.TryAdd(shaderDef.Name, shaderDef);

        if (shaderImportIds.TryGetValue(key, out var id))
            return id;

        ThrowIfFrozen();
        return ImportShaderType(shaderDef, key);
    }

    // Resolve a shader's generic arguments to build a string key like "Shader<3,true>".
    // Returns null if any generic arg can't be evaluated to a concrete value.
    private static string? ResolveShaderStringKey(string name, ConstantExpression[] genericArguments)
    {
        if (genericArguments.Length == 0)
            return name;
        var args = new string[genericArguments.Length];
        for (int j = 0; j < genericArguments.Length; j++)
        {
            if (!genericArguments[j].TryEvaluate(out var value) || value is null)
                return null;
            args[j] = ShaderClassSource.ConvertGenericArgToString(value);
        }
        return $"{name}<{string.Join(",", args)}>";
    }

    public void ReplaceType(SymbolType type, int id)
    {
        ThrowIfFrozen();
        RemoveType(id);
        RegisterType(type, id);
    }

    public int RemoveType(SymbolType type)
    {
        ThrowIfFrozen();
        var typeId = Types[type];
        RemoveType(typeId);
        return typeId;
    }

    public void RemoveType(int typeId)
    {
        ThrowIfFrozen();
        foreach (var i in Buffer)
        {
            if (i.Data.IdResult == typeId)
            {
                SpirvBuilder.SetOpNop(i.Data.Memory.Span);
                var type = ReverseTypes[typeId];
                Types.Remove(type);
                ReverseTypes.Remove(typeId);
                return;
            }
        }

        throw new InvalidOperationException($"Type to remove {typeId} was not found");
    }

    public int RegisterType(SymbolType type, int id)
    {
        var instruction = type switch
        {
            ScalarType s =>
                s.Type switch
                {
                    Scalar.Void => Buffer.AddData(new OpTypeVoid(id)).IdResult,
                    Scalar.Boolean => Buffer.AddData(new OpTypeBool(id)).IdResult,
                    Scalar.Int => Buffer.AddData(new OpTypeInt(id, 32, 1)).IdResult,
                    Scalar.UInt => Buffer.AddData(new OpTypeInt(id, 32, 0)).IdResult,
                    Scalar.Int64 => Buffer.AddData(new OpTypeInt(id, 64, 1)).IdResult,
                    Scalar.UInt64 => Buffer.AddData(new OpTypeInt(id, 64, 0)).IdResult,
                    Scalar.Half => Buffer.AddData(new OpTypeFloat(id, 16, null)).IdResult,
                    Scalar.Float => Buffer.AddData(new OpTypeFloat(id, 32, null)).IdResult,
                    Scalar.Double => Buffer.AddData(new OpTypeFloat(id, 64, null)).IdResult,
                    _ => throw new NotImplementedException($"Can't add type {type}")
                },
            VectorType v => Buffer.AddData(new OpTypeVector(id, GetOrRegister(v.BaseType), v.Size)).IdResult,
            MatrixType m => Buffer
                .AddData(new OpTypeMatrix(id, GetOrRegister(new VectorType(m.BaseType, m.Rows)), m.Columns))
                .IdResult,
            ArrayType a when a.Size != -1 || a.SizeExpression != null => RegisterArrayType(a),
            ArrayType a when a.Size == -1 && a.SizeExpression == null => Buffer
                .AddData(new OpTypeRuntimeArray(id, GetOrRegister(a.BaseType))).IdResult,
            StructType st => RegisterStructuredType(st.ToId(), st),
            FunctionType f => RegisterFunctionType(f, id),
            PointerType p => RegisterPointerType(p, id),
            // SampledType stores the full return type (e.g. float4, not just float) so that
            // Texture<float> and Texture<float4> produce structurally distinct OpTypeImage during merge.
            // ShaderMixer normalizes SampledType back to scalar before final SPIR-V emission.
            Texture1DType t => Buffer.AddData(new OpTypeImage(id, GetOrRegister(t.ReturnType), t.Dimension,
                t.Depth, t.Arrayed ? 1 : 0, t.Multisampled ? 1 : 0, t.Sampled, t.Sampled == 2 ? GetStorageImageFormat(t.ReturnType) : t.Format, null)).IdResult,
            Texture2DType t => Buffer.AddData(new OpTypeImage(id, GetOrRegister(t.ReturnType), t.Dimension,
                t.Depth, t.Arrayed ? 1 : 0, t.Multisampled ? 1 : 0, t.Sampled, t.Sampled == 2 ? GetStorageImageFormat(t.ReturnType) : t.Format, null)).IdResult,
            Texture3DType t => Buffer.AddData(new OpTypeImage(id, GetOrRegister(t.ReturnType), t.Dimension,
                t.Depth, t.Arrayed ? 1 : 0, t.Multisampled ? 1 : 0, t.Sampled, t.Sampled == 2 ? GetStorageImageFormat(t.ReturnType) : t.Format, null)).IdResult,
            TextureCubeType t => Buffer.AddData(new OpTypeImage(id, GetOrRegister(t.ReturnType), t.Dimension,
                t.Depth, t.Arrayed ? 1 : 0, t.Multisampled ? 1 : 0, t.Sampled, t.Sampled == 2 ? GetStorageImageFormat(t.ReturnType) : t.Format, null)).IdResult,
            SamplerType st => Buffer.AddData(new OpTypeSampler(id)).IdResult,
            BufferType b => Buffer.AddData(new OpTypeImage(id, GetOrRegister(b.BaseType), Specification.Dim.Buffer,
                2, 0, 0, b.WriteAllowed ? 2 : 1, Specification.ImageFormat.Unknown, null)).IdResult,
            AppendStructuredBufferType ab => RegisterAppendOrConsumeStructuredBufferType("Append", ab.BaseType),
            ConsumeStructuredBufferType cb => RegisterAppendOrConsumeStructuredBufferType("Consume", cb.BaseType),
            StructuredBufferType b => RegisterStructuredBufferType(b),
            ByteAddressBufferType b => RegisterByteAddressBufferType(b),
            SampledImage si => Buffer.AddData(new OpTypeSampledImage(id, GetOrRegister(si.ImageType))).IdResult,
            GenericParameterType g => Buffer.AddData(new OpTypeGenericSDSL(id, g.Kind)).IdResult,
            StreamsType s => Buffer.AddData(new OpTypeStreamsSDSL(id, s.Kind)).IdResult,
            GeometryStreamType so => Buffer.AddData(new OpTypeGeometryStreamOutputSDSL(id, GetOrRegister(so.BaseType), so.Kind)).IdResult,
            PatchType patch => Buffer.AddData(new OpTypePatchSDSL(id, GetOrRegister(patch.BaseType), patch.Kind, patch.Size)).IdResult,
            // StructSymbol st => RegisterStruct(st),
            _ => throw new NotImplementedException($"Can't add type {type}")
        };
        Types[type] = instruction ?? -1;
        ReverseTypes[instruction ?? -1] = type;
        return instruction ?? -1;
    }

    private int RegisterStructuredBufferType(StructuredBufferType structuredBufferType)
    {
        var elementSize = SpirvBuilder.StorageBufferArrayStride(structuredBufferType.BaseType);
        var runtimeArrayType = GetOrCreateRuntimeArray(GetOrRegister(structuredBufferType.BaseType), elementSize);

        var bufferType = Buffer.Add(new OpTypeStruct(Bound++, [runtimeArrayType])).ResultId;
        AddName(bufferType, $"type.{(structuredBufferType.WriteAllowed ? "RW" : "")}StructuredBuffer.{structuredBufferType.BaseType.ToId()}");
        Buffer.Add(new OpMemberDecorate(bufferType, 0, Specification.Decoration.Offset, [0]));
        Buffer.Add(new OpDecorate(bufferType, Specification.Decoration.Block, []));

        return bufferType;
    }

    private int RegisterAppendOrConsumeStructuredBufferType(string prefix, SymbolType baseType)
    {
        var elementSize = SpirvBuilder.StorageBufferArrayStride(baseType);
        var runtimeArrayType = GetOrCreateRuntimeArray(GetOrRegister(baseType), elementSize);

        var bufferType = Buffer.Add(new OpTypeStruct(Bound++, [runtimeArrayType])).ResultId;
        AddName(bufferType, $"type.{prefix}StructuredBuffer.{baseType.ToId()}");
        Buffer.Add(new OpMemberDecorate(bufferType, 0, Specification.Decoration.Offset, [0]));
        Buffer.Add(new OpDecorate(bufferType, Specification.Decoration.Block, []));

        return bufferType;
    }

    private int RegisterByteAddressBufferType(ByteAddressBufferType byteAddressBufferType)
    {
        var uintTypeId = GetOrRegister(ScalarType.UInt);
        var runtimeArrayType = Buffer.Add(new OpTypeRuntimeArray(Bound++, uintTypeId)).ResultId;
        Buffer.Add(new OpDecorate(runtimeArrayType, Specification.Decoration.ArrayStride, [4]));

        var bufferType = Buffer.Add(new OpTypeStruct(Bound++, [runtimeArrayType])).ResultId;
        AddName(bufferType, $"type.{(byteAddressBufferType.WriteAllowed ? "RW" : "")}ByteAddressBuffer");
        Buffer.Add(new OpMemberDecorate(bufferType, 0, Specification.Decoration.Offset, [0]));
        Buffer.Add(new OpDecorate(bufferType, Specification.Decoration.Block, []));

        return bufferType;
    }

    private int RegisterArrayType(ArrayType a)
    {
        int sizeId;
        if (a.Size != -1)
            sizeId = CompileConstant((int)a.Size).Id;
        else if (a.SizeExpression is { } expr)
            sizeId = expr.Emit(this);
        else
            throw new InvalidOperationException();

        return Buffer.Add(new OpTypeArray(Bound++, GetOrRegister(a.BaseType), sizeId)).ResultId;
    }

    private int ImportShaderType(ShaderDefinition shaderSymbol, string key)
    {
        var id = Bound++;
        var emittedArgs = new int[shaderSymbol.GenericArguments.Length];
        for (int i = 0; i < emittedArgs.Length; i++)
            emittedArgs[i] = shaderSymbol.GenericArguments[i].Emit(this);
        Add(new OpImportShaderSDSL(id, new(shaderSymbol.Name), new(emittedArgs.AsSpan())));
        AddName(id, shaderSymbol.Name);
        shaderImportIds[key] = id;

        // Import struct types — these ARE real SPIR-V types and belong in Types/ReverseTypes
        var structTypes = CollectionsMarshal.AsSpan(shaderSymbol.StructTypes);
        foreach (ref var structType in structTypes)
        {
            ImportShaderStruct(id, structType.Type, out structType.ImportedId);
        }

        // Note: Variables and methods are imported lazily in ShaderDefinition.TryResolveSymbol()

        return id;
    }

    private void ImportShaderStruct(int shaderId, StructuredType structType, out int structImportedId)
    {
        var @struct = Add(new OpImportStructSDSL(Bound++, structType.ToId(), shaderId));
        AddName(@struct.ResultId, structType.Name);

        // Fill the ID
        structImportedId = @struct.ResultId;

        // Register it so that it can be used right after during OpVariable for cbuffer
        Types.Add(structType, structImportedId);
        ReverseTypes.Add(structImportedId, structType);
    }

    public void ImportShaderVariable(int shaderId, ref Symbol symbol, Specification.VariableFlagsMask flags)
    {
        symbol.IdRef = Bound++;
        Add(new OpImportVariableSDSL(symbol.IdRef, GetOrRegister(symbol.Type), symbol.Id.Name, shaderId, flags));
        AddName(symbol.IdRef, symbol.Id.Name);
    }

    public void ImportShaderMethod(int shaderId, ref Symbol symbol, Specification.FunctionFlagsMask flags)
    {
        var functionType = (FunctionType)symbol.Type;
        var functionTypeId = GetOrRegister(functionType);

        symbol.IdRef = Bound++;
        Add(new OpImportFunctionSDSL(symbol.IdRef, functionTypeId, symbol.Id.Name, shaderId, flags));
        AddName(symbol.IdRef, symbol.Id.Name);
    }

    public int DeclareCBuffer(ConstantBufferSymbol cb, int id)
    {
        var result = DeclareStructuredType(cb, id);

        Buffer.Add(new OpDecorate(result, Specification.Decoration.Block, []));

        return result;
    }

    private int RegisterStructuredType(string name, StructuredType structSymbol)
    {
        throw new InvalidOperationException();
    }

    public int DeclareStructuredType(StructuredType structSymbol, int id)
    {
        Span<int> types = stackalloc int[structSymbol.Members.Count];
        for (var index = 0; index < structSymbol.Members.Count; index++)
            types[index] = GetOrRegister(structSymbol.Members[index].Type);

        Add(new OpTypeStruct(id, [.. types]));
        AddName(id, structSymbol.ToId());
        for (var index = 0; index < structSymbol.Members.Count; index++)
        {
            var member = structSymbol.Members[index];
            AddMemberName(id, index, member.Name);
        }

        Types[structSymbol] = id;
        ReverseTypes[id] = structSymbol;

        return id;
    }

    private int RegisterFunctionType(FunctionType functionType, int id)
    {
        Span<(int, int)> types = stackalloc (int, int)[functionType.ParameterTypes.Count];
        for (int i = 0; i < functionType.ParameterTypes.Count; i++)
        {
            types[i].Item1 = GetOrRegister(functionType.ParameterTypes[i].Type);
            types[i].Item2 = (int)functionType.ParameterTypes[i].Modifiers;
        }

        Buffer.Add(new OpTypeFunctionSDSL(id, GetOrRegister(functionType.ReturnType), [.. types]));
        // disabled for now: currently it generates name with {}, not working with most SPIRV tools
        // AddName(result, functionType.ToId());
        return id;
    }

    private int RegisterPointerType(PointerType pointerType, int id)
    {
        var baseType = GetOrRegister(pointerType.BaseType);
        Add(new OpTypePointer(id, pointerType.StorageClass, baseType));
        AddName(id, pointerType.ToId());
        return id;
    }

    // Derives the SPIR-V ImageFormat for a storage (RW) texture from its return type.
    // Note: 3-component float/int/uint formats don't exist in SPIR-V (no Rgb32f etc.).
    private static Specification.ImageFormat GetStorageImageFormat(SymbolType returnType)
    {
        var scalar = returnType.GetElementType();
        int count = returnType.GetElementCount();
        return (scalar.Type, count) switch
        {
            (Scalar.Float, 1) => Specification.ImageFormat.R32f,
            (Scalar.Float, 2) => Specification.ImageFormat.Rg32f,
            (Scalar.Float, 4) => Specification.ImageFormat.Rgba32f,
            (Scalar.Float, 3) => Specification.ImageFormat.Unknown,
            (Scalar.UInt, 1) => Specification.ImageFormat.R32ui,
            (Scalar.UInt, 2) => Specification.ImageFormat.Rg32ui,
            (Scalar.UInt, 4) => Specification.ImageFormat.Rgba32ui,
            (Scalar.Int, 1) => Specification.ImageFormat.R32i,
            (Scalar.Int, 2) => Specification.ImageFormat.Rg32i,
            (Scalar.Int, 4) => Specification.ImageFormat.Rgba32i,
            _ => Specification.ImageFormat.Unknown,
        };
    }
}

using System.Runtime.InteropServices;
using Stride.Shaders.Core;
using Stride.Shaders.Spirv.Core;

namespace Stride.Shaders.Spirv.Building;

public partial class SpirvContext
{
    public int GetOrRegister(SymbolType? type)
    {
        if (type is null)
            throw new ArgumentException($"Type is null");
        if (Types.TryGetValue(type, out var res))
            return res;

        return RegisterType(type, Bound++);
    }

    public void ReplaceType(SymbolType type, int id)
    {
        RemoveType(id);
        RegisterType(type, id);
    }

    public int RemoveType(SymbolType type)
    {
        var typeId = Types[type];
        RemoveType(typeId);
        return typeId;
    }
    
    public void RemoveType(int typeId)
    {
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
                    Scalar.Void => Buffer.Add(new OpTypeVoid(id)).IdResult,
                    Scalar.Boolean => Buffer.Add(new OpTypeBool(id)).IdResult,
                    Scalar.Int => Buffer.Add(new OpTypeInt(id, 32, 1)).IdResult,
                    Scalar.UInt => Buffer.Add(new OpTypeInt(id, 32, 0)).IdResult,
                    Scalar.Int64 => Buffer.Add(new OpTypeInt(id, 64, 1)).IdResult,
                    Scalar.UInt64 => Buffer.Add(new OpTypeInt(id, 64, 0)).IdResult,
                    Scalar.Float => Buffer.Add(new OpTypeFloat(id, 32, null)).IdResult,
                    Scalar.Double => Buffer.Add(new OpTypeFloat(id, 64, null)).IdResult,
                    _ => throw new NotImplementedException($"Can't add type {type}")
                },
            VectorType v => Buffer.Add(new OpTypeVector(id, GetOrRegister(v.BaseType), v.Size)).IdResult,
            MatrixType m => Buffer
                .Add(new OpTypeMatrix(id, GetOrRegister(new VectorType(m.BaseType, m.Rows)), m.Columns))
                .IdResult,
            ArrayType a when a.Size != -1 || a.SizeExpression != null => RegisterArrayType(a),
            ArrayType a when a.Size == -1 && a.SizeExpression == null => Buffer
                .Add(new OpTypeRuntimeArray(id, GetOrRegister(a.BaseType))).IdResult,
            StructType st => RegisterStructuredType(st.ToId(), st),
            FunctionType f => RegisterFunctionType(f, id),
            PointerType p => RegisterPointerType(p, id),
            LoadedShaderSymbol s => ImportShaderType(s, id),
            Texture1DType t => Buffer.Add(new OpTypeImage(id, GetOrRegister(t.ReturnType), t.Dimension,
                t.Depth, t.Arrayed ? 1 : 0, t.Multisampled ? 1 : 0, t.Sampled, t.Format, null)).IdResult,
            Texture2DType t => Buffer.Add(new OpTypeImage(id, GetOrRegister(t.ReturnType), t.Dimension,
                t.Depth, t.Arrayed ? 1 : 0, t.Multisampled ? 1 : 0, t.Sampled, t.Format, null)).IdResult,
            Texture3DType t => Buffer.Add(new OpTypeImage(id, GetOrRegister(t.ReturnType), t.Dimension,
                t.Depth, t.Arrayed ? 1 : 0, t.Multisampled ? 1 : 0, t.Sampled, t.Format, null)).IdResult,
            TextureCubeType t => Buffer.Add(new OpTypeImage(id, GetOrRegister(t.ReturnType), t.Dimension,
                t.Depth, t.Arrayed ? 1 : 0, t.Multisampled ? 1 : 0, t.Sampled, t.Format, null)).IdResult,
            SamplerType st => Buffer.Add(new OpTypeSampler(id)).IdResult,
            BufferType b => Buffer.Add(new OpTypeImage(id, GetOrRegister(b.BaseType), Specification.Dim.Buffer,
                2, 0, 0, b.WriteAllowed ? 2 : 1, Specification.ImageFormat.Unknown, null)).IdResult,
            StructuredBufferType b => RegisterStructuredBufferType(b),
            SampledImage si => Buffer.Add(new OpTypeSampledImage(id, GetOrRegister(si.ImageType))).IdResult,
            GenericParameterType g => Buffer.Add(new OpTypeGenericSDSL(id, g.Kind)).IdResult,
            StreamsType s => Buffer.Add(new OpTypeStreamsSDSL(id, s.Kind)).IdResult,
            GeometryStreamType so => Buffer.Add(new OpTypeGeometryStreamOutputSDSL(id, GetOrRegister(so.BaseType), so.Kind)).IdResult,
            PatchType patch => Buffer.Add(new OpTypePatchSDSL(id, GetOrRegister(patch.BaseType), patch.Kind, patch.Size)).IdResult,
            // StructSymbol st => RegisterStruct(st),
            _ => throw new NotImplementedException($"Can't add type {type}")
        };
        Types[type] = instruction ?? -1;
        ReverseTypes[instruction ?? -1] = type;
        return instruction ?? -1;
    }

    private int RegisterStructuredBufferType(StructuredBufferType structuredBufferType)
    {
        var runtimeArrayType = Buffer.Add(new OpTypeRuntimeArray(Bound++, GetOrRegister(structuredBufferType.BaseType))).IdResult.Value;
        
        var bufferType = Buffer.Add(new OpTypeStruct(Bound++, [runtimeArrayType])).IdResult.Value;
        AddName(bufferType, $"type.{(structuredBufferType.WriteAllowed ? "RW" : "")}StructuredBuffer.{structuredBufferType.BaseType.ToId()}");
        Buffer.Add(new OpMemberDecorate(bufferType, 0, Specification.Decoration.Offset, [0]));
        
        // TODO: Add array stride and offsets
        Buffer.Add(new OpDecorate(bufferType, Specification.Decoration.Block, []));

        return bufferType;
    }

    private int? RegisterArrayType(ArrayType a)
    {
        int sizeId;
        if (a.Size != -1)
        {
            sizeId = CompileConstant((int)a.Size).Id;
        }
        else if (a.SizeExpression is { } sizeExpression)
        {
            // Import constants
            var importBuffer = sizeExpression.Buffer;
            if (importBuffer != Buffer)
            {
                var resultId = InsertWithoutDuplicates(null, importBuffer);
                a.SizeExpression = (resultId, Buffer);
                // Now that we reference a constant in context buffer,
                // check again if array is not already added (if constants are unified, it should work)
                if (Types.TryGetValue(a, out var res))
                    return res;
                sizeId = resultId;
            }
            else
            {
                sizeId = sizeExpression.Id;
            }
        }
        else
        {
            throw new InvalidOperationException();
        }

        return Buffer.Add(new OpTypeArray(Bound++, GetOrRegister(a.BaseType), sizeId)).IdResult;
    }

    public int ImportShaderType(LoadedShaderSymbol shaderSymbol, int id)
    {
        Add(new OpSDSLImportShader(id, new(shaderSymbol.Name), new(shaderSymbol.GenericArguments.AsSpan())));
        AddName(id, shaderSymbol.Name);

        // Import struct
        var structTypes = CollectionsMarshal.AsSpan(shaderSymbol.StructTypes);
        foreach (ref var structType in structTypes)
        {
            ImportShaderStruct(id, structType.Type, out structType.ImportedId);
        }

        // Note: Variables and methods are imported lazily in LoadedShaderSymbol.TryResolveSymbol()

        return id;
    }

    private void ImportShaderStruct(int shaderId, StructuredType structType, out int structImportedId)
    {
        FluentAdd(new OpSDSLImportStruct(Bound++, structType.ToId(), shaderId), out var @struct);
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
        Add(new OpSDSLImportVariable(symbol.IdRef, GetOrRegister(symbol.Type), symbol.Id.Name, shaderId, flags));
        AddName(symbol.IdRef, symbol.Id.Name);
    }

    public void ImportShaderMethod(int shaderId, ref Symbol symbol, Specification.FunctionFlagsMask flags)
    {
        var functionType = (FunctionType)symbol.Type;
        var functionTypeId = GetOrRegister(functionType);

        symbol.IdRef = Bound++;
        Add(new OpSDSLImportFunction(symbol.IdRef, functionTypeId, symbol.Id.Name, shaderId, flags));
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
}
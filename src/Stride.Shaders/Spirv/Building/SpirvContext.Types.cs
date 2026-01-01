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
        else
        {
            var instruction = type switch
            {
                ScalarType s =>
                    s.TypeName switch
                    {
                        "void" => Buffer.Add(new OpTypeVoid(Bound++)).IdResult,
                        "bool" => Buffer.Add(new OpTypeBool(Bound++)).IdResult,
                        "sbyte" => Buffer.Add(new OpTypeInt(Bound++, 8, 1)).IdResult,
                        "byte" => Buffer.Add(new OpTypeInt(Bound++, 8, 0)).IdResult,
                        "ushort" => Buffer.Add(new OpTypeInt(Bound++, 16, 1)).IdResult,
                        "short" => Buffer.Add(new OpTypeInt(Bound++, 16, 0)).IdResult,
                        "int" => Buffer.Add(new OpTypeInt(Bound++, 32, 1)).IdResult,
                        "uint" => Buffer.Add(new OpTypeInt(Bound++, 32, 0)).IdResult,
                        "long" => Buffer.Add(new OpTypeInt(Bound++, 64, 1)).IdResult,
                        "ulong" => Buffer.Add(new OpTypeInt(Bound++, 64, 0)).IdResult,
                        "half" => Buffer.Add(new OpTypeFloat(Bound++, 16, null)).IdResult,
                        "float" => Buffer.Add(new OpTypeFloat(Bound++, 32, null)).IdResult,
                        "double" => Buffer.Add(new OpTypeFloat(Bound++, 64, null)).IdResult,
                        _ => throw new NotImplementedException($"Can't add type {type}")
                    },
                VectorType v => Buffer.Add(new OpTypeVector(Bound++, GetOrRegister(v.BaseType), v.Size)).IdResult,
                MatrixType m => Buffer
                    .Add(new OpTypeMatrix(Bound++, GetOrRegister(new VectorType(m.BaseType, m.Rows)), m.Columns))
                    .IdResult,
                ArrayType a when a.Size != -1 || a.SizeExpression != null => RegisterArrayType(a),
                ArrayType a when a.Size == -1 && a.SizeExpression == null => Buffer
                    .Add(new OpTypeRuntimeArray(Bound++, GetOrRegister(a.BaseType))).IdResult,
                StructType st => RegisterStructuredType(st.ToId(), st),
                FunctionType f => RegisterFunctionType(f),
                PointerType p => RegisterPointerType(p),
                LoadedShaderSymbol s => ImportShaderType(s),
                Texture1DType t => Buffer.Add(new OpTypeImage(Bound++, GetOrRegister(t.ReturnType), t.Dimension,
                    t.Depth, t.Arrayed ? 1 : 0, t.Multisampled ? 1 : 0, t.Sampled, t.Format, null)).IdResult,
                Texture2DType t => Buffer.Add(new OpTypeImage(Bound++, GetOrRegister(t.ReturnType), t.Dimension,
                    t.Depth, t.Arrayed ? 1 : 0, t.Multisampled ? 1 : 0, t.Sampled, t.Format, null)).IdResult,
                Texture3DType t => Buffer.Add(new OpTypeImage(Bound++, GetOrRegister(t.ReturnType), t.Dimension,
                    t.Depth, t.Arrayed ? 1 : 0, t.Multisampled ? 1 : 0, t.Sampled, t.Format, null)).IdResult,
                TextureCubeType t => Buffer.Add(new OpTypeImage(Bound++, GetOrRegister(t.ReturnType), t.Dimension,
                    t.Depth, t.Arrayed ? 1 : 0, t.Multisampled ? 1 : 0, t.Sampled, t.Format, null)).IdResult,
                SamplerType st => Buffer.Add(new OpTypeSampler(Bound++)).IdResult,
                BufferType b => Buffer.Add(new OpTypeImage(Bound++, GetOrRegister(b.BaseType), Specification.Dim.Buffer,
                    2, 0, 0, 1, Specification.ImageFormat.Unknown, null)).IdResult,
                SampledImage si => Buffer.Add(new OpTypeSampledImage(Bound++, GetOrRegister(si.ImageType))).IdResult,
                GenericParameterType g => Buffer.Add(new OpTypeGenericSDSL(Bound++, g.Kind)).IdResult,
                StreamsType s => Buffer.Add(new OpTypeStreamsSDSL(Bound++)).IdResult,
                // StructSymbol st => RegisterStruct(st),
                _ => throw new NotImplementedException($"Can't add type {type}")
            };
            Types[type] = instruction ?? -1;
            ReverseTypes[instruction ?? -1] = type;
            return instruction ?? -1;
        }
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

    public int ImportShaderType(LoadedShaderSymbol shaderSymbol)
    {
        FluentAdd(new OpSDSLImportShader(Bound++, new(shaderSymbol.Name), new(shaderSymbol.GenericArguments.AsSpan())),
            out var shader);
        AddName(shader.ResultId, shaderSymbol.Name);

        // Import struct
        var structTypes = CollectionsMarshal.AsSpan(shaderSymbol.StructTypes);
        foreach (ref var structType in structTypes)
        {
            ImportShaderStruct(shader, structType.Type, out structType.ImportedId);
        }

        // Note: Variables and methods are imported lazily in LoadedShaderSymbol.TryResolveSymbol()

        return shader.ResultId;
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

    public int DeclareCBuffer(ConstantBufferSymbol cb)
    {
        var result = DeclareStructuredType(cb);

        Buffer.Add(new OpDecorate(result, Specification.Decoration.Block));

        return result;
    }

    private int RegisterStructuredType(string name, StructuredType structSymbol)
    {
        throw new InvalidOperationException();
    }

    public int DeclareStructuredType(StructuredType structSymbol)
    {
        Span<int> types = stackalloc int[structSymbol.Members.Count];
        for (var index = 0; index < structSymbol.Members.Count; index++)
            types[index] = GetOrRegister(structSymbol.Members[index].Type);

        var result = Add(new OpTypeStruct(Bound++, [.. types]));
        var id = result.IdResult ?? throw new InvalidOperationException();
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

    private int RegisterFunctionType(FunctionType functionType)
    {
        Span<(int, int)> types = stackalloc (int, int)[functionType.ParameterTypes.Count];
        for (int i = 0; i < functionType.ParameterTypes.Count; i++)
        {
            types[i].Item1 = GetOrRegister(functionType.ParameterTypes[i].Type);
            types[i].Item2 = (int)functionType.ParameterTypes[i].Modifiers;
        }

        var result = Buffer.Add(new OpTypeFunctionSDSL(Bound++, GetOrRegister(functionType.ReturnType), [.. types]));
        // disabled for now: currently it generates name with {}, not working with most SPIRV tools
        // AddName(result, functionType.ToId());
        return result.IdResult ?? -1;
    }

    private int RegisterPointerType(PointerType pointerType)
    {
        var baseType = GetOrRegister(pointerType.BaseType);
        var result = Add(new OpTypePointer(Bound++, pointerType.StorageClass, baseType));
        var id = result.IdResult;
        AddName(id ?? -1, pointerType.ToId());
        return id ?? -1;
    }
}
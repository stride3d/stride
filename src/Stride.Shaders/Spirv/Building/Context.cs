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
public class SpirvContext
{
    private int bound = 1;
    public int ResourceGroupBound { get; set; } = 1;
    public ref int Bound => ref bound;
    public Dictionary<SymbolType, int> Types { get; init; } = [];
    public Dictionary<int, SymbolType> ReverseTypes { get; init; } = [];
    public Dictionary<int, string> Names { get; init; } = [];
    public Dictionary<(SymbolType Type, object Value), SpirvValue> LiteralConstants { get; } = [];
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

    public int AddConstant<TScalar>(TScalar value)
        where TScalar : INumber<TScalar>
    {
        var data = value switch
        {
            byte v => Buffer.Add(new OpConstant<byte>(GetOrRegister(ScalarType.From("byte")), Bound++, v)),
            sbyte v => Buffer.Add(new OpConstant<sbyte>(GetOrRegister(ScalarType.From("sbyte")), Bound++, v)),
            ushort v => Buffer.Add(new OpConstant<ushort>(GetOrRegister(ScalarType.From("ushort")), Bound++, v)),
            short v => Buffer.Add(new OpConstant<short>(GetOrRegister(ScalarType.From("short")), Bound++, v)),
            uint v => Buffer.Add(new OpConstant<uint>(GetOrRegister(ScalarType.From("uint")), Bound++, v)),
            int v => Buffer.Add(new OpConstant<int>(GetOrRegister(ScalarType.From("int")), Bound++, v)),
            ulong v => Buffer.Add(new OpConstant<ulong>(GetOrRegister(ScalarType.From("ulong")), Bound++, v)),
            long v => Buffer.Add(new OpConstant<long>(GetOrRegister(ScalarType.From("long")), Bound++, v)),
            Half v => Buffer.Add(new OpConstant<Half>(GetOrRegister(ScalarType.From("half")), Bound++, v)),
            float v => Buffer.Add(new OpConstant<float>(GetOrRegister(ScalarType.From("float")), Bound++, v)),
            double v => Buffer.Add(new OpConstant<double>(GetOrRegister(ScalarType.From("bdouble")), Bound++, v)),
            _ => throw new NotImplementedException()
        };
        if (InstructionInfo.GetInfo(data).GetResultIndex(out var index))
            return data.Memory.Span[index + 1];
        throw new Exception("Constant has no result id");
    }

    public void SetEntryPoint(ExecutionModel model, int function, string name, ReadOnlySpan<Symbol> variables)
    {
        Span<int> pvariables = stackalloc int[variables.Length];
        int pos = 0;
        foreach (var v in variables)
            pvariables[pos++] = v.IdRef;
        Buffer.Add(new OpEntryPoint(model, function, name, [.. pvariables]));
    }

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
                MatrixType m => Buffer.Add(new OpTypeMatrix(Bound++, GetOrRegister(new VectorType(m.BaseType, m.Rows)), m.Columns)).IdResult,
                ArrayType a when a.Size != -1 || a.SizeExpression != null => RegisterArrayType(a),
                ArrayType a when a.Size == -1 && a.SizeExpression == null => Buffer.Add(new OpTypeRuntimeArray(Bound++, GetOrRegister(a.BaseType))).IdResult,
                StructType st => RegisterStructuredType(st.ToId(), st),
                FunctionType f => RegisterFunctionType(f),
                PointerType p => RegisterPointerType(p),
                LoadedShaderSymbol s => ImportShaderType(s),
                Texture1DType t => Buffer.Add(new OpTypeImage(Bound++, GetOrRegister(t.ReturnType), t.Dimension, t.Depth, t.Arrayed ? 1 : 0, t.Multisampled ? 1 : 0, t.Sampled, t.Format, null)).IdResult,
                Texture2DType t => Buffer.Add(new OpTypeImage(Bound++, GetOrRegister(t.ReturnType), t.Dimension, t.Depth, t.Arrayed ? 1 : 0, t.Multisampled ? 1 : 0, t.Sampled, t.Format, null)).IdResult,
                Texture3DType t => Buffer.Add(new OpTypeImage(Bound++, GetOrRegister(t.ReturnType), t.Dimension, t.Depth, t.Arrayed ? 1 : 0, t.Multisampled ? 1 : 0, t.Sampled, t.Format, null)).IdResult,
                TextureCubeType t => Buffer.Add(new OpTypeImage(Bound++, GetOrRegister(t.ReturnType), t.Dimension, t.Depth, t.Arrayed ? 1 : 0, t.Multisampled ? 1 : 0, t.Sampled, t.Format, null)).IdResult,
                SamplerType st => Buffer.Add(new OpTypeSampler(Bound++)).IdResult,
                BufferType b => Buffer.Add(new OpTypeImage(Bound++, GetOrRegister(b.BaseType), Dim.Buffer, 2, 0, 0, 1, ImageFormat.Unknown, null)).IdResult,
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
        FluentAdd(new OpSDSLImportShader(Bound++, new(shaderSymbol.Name), new(shaderSymbol.GenericArguments.AsSpan())), out var shader);
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

    public void ImportShaderVariable(int shaderId, ref Symbol symbol, VariableFlagsMask flags)
    {
        symbol.IdRef = Bound++;
        Add(new OpSDSLImportVariable(symbol.IdRef, GetOrRegister(symbol.Type), symbol.Id.Name, shaderId, flags));
        AddName(symbol.IdRef, symbol.Id.Name);
    }

    public void ImportShaderMethod(int shaderId, ref Symbol symbol, FunctionFlagsMask flags)
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

        Buffer.Add(new OpDecorate(result, Decoration.Block));

        return result;
    }

    int RegisterStructuredType(string name, StructuredType structSymbol)
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


    int RegisterFunctionType(FunctionType functionType)
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

    int RegisterPointerType(PointerType pointerType)
    {
        var baseType = GetOrRegister(pointerType.BaseType);
        var result = Add(new OpTypePointer(Bound++, pointerType.StorageClass, baseType));
        var id = result.IdResult;
        AddName(id ?? -1, pointerType.ToId());
        return id ?? -1;
    }

    public unsafe SpirvValue CreateConstantCompositeRepeat(Literal literal, int size)
    {
        var value = CompileConstantLiteral(literal);
        if (size == 1)
            return value;

        Span<int> values = stackalloc int[size];
        for (int i = 0; i < size; ++i)
            values[i] = size;
        
        var type = new VectorType((ScalarType)ReverseTypes[value.TypeId], size);
        var instruction = Buffer.Add(new OpConstantComposite(GetOrRegister(type), Bound++, new(values)));

        return new(instruction);
    }

    public Literal CreateLiteral(object value, TextLocation location = default)
    {
        return value switch
        {
            bool i => new BoolLiteral(i, location),
            sbyte i => new IntegerLiteral(new(8, false, true), i, location),
            byte i => new IntegerLiteral(new(8, false, false), i, location),
            short i => new IntegerLiteral(new(16, false, true), i, location),
            ushort i => new IntegerLiteral(new(16, false, false), i, location),
            int i => new IntegerLiteral(new(32, false, true), i, location),
            uint i => new IntegerLiteral(new(32, false, false), i, location),
            long i => new IntegerLiteral(new(64, false, true), i, location),
            ulong i => new IntegerLiteral(new(64, false, false), (long)i, location),
            float i => new FloatLiteral(new(32, true, true), i, null, location),
            double i => new FloatLiteral(new(64, true, true), i, null, location),
        };
    }

    public SpirvValue CompileConstant(object value, TextLocation location = default)
    {
        return CompileConstantLiteral(CreateLiteral(value, location));
    }

    public SpirvValue CompileConstantLiteral(Literal literal)
    {
        object literalValue = literal switch
        {
            BoolLiteral lit => lit.Value,
            IntegerLiteral lit => lit.Suffix.Size switch
            {
                > 32 => lit.LongValue,
                _ => lit.IntValue,
            },
            FloatLiteral lit => lit.Suffix.Size switch
            {
                > 32 => lit.DoubleValue,
                _ => (float)lit.DoubleValue,
            },
        };

        if (literal.Type == null)
        {
            literal.Type = literal switch
            {
                BoolLiteral lit => ScalarType.From("bool"),
                IntegerLiteral lit => lit.Suffix switch
                {
                    { Signed: true, Size: 8 } => ScalarType.From("sbyte"),
                    { Signed: true, Size: 16 } => ScalarType.From("short"),
                    { Signed: true, Size: 32 } => ScalarType.From("int"),
                    { Signed: true, Size: 64 } => ScalarType.From("long"),
                    { Signed: false, Size: 8 } => ScalarType.From("byte"),
                    { Signed: false, Size: 16 } => ScalarType.From("ushort"),
                    { Signed: false, Size: 32 } => ScalarType.From("uint"),
                    { Signed: false, Size: 64 } => ScalarType.From("ulong"),
                    _ => throw new NotImplementedException("Unsupported integer suffix")
                },
                FloatLiteral lit => lit.Suffix.Size switch
                {
                    16 => ScalarType.From("half"),
                    32 => ScalarType.From("float"),
                    64 => ScalarType.From("double"),
                    _ => throw new NotImplementedException("Unsupported float")
                },
            };
        }

        if (LiteralConstants.TryGetValue((literal.Type, literalValue), out var result))
            return result;

        var instruction = literal switch
        {
            BoolLiteral { Value: true } lit => Buffer.Add(new OpConstantTrue(GetOrRegister(lit.Type), Bound++)),
            BoolLiteral { Value: false } lit => Buffer.Add(new OpConstantFalse(GetOrRegister(lit.Type), Bound++)),
            IntegerLiteral lit => lit.Suffix switch
            {
                { Size: <= 8, Signed: false } => Buffer.Add(new OpConstant<byte>(GetOrRegister(lit.Type), Bound++, (byte)lit.IntValue)),
                { Size: <= 8, Signed: true } => Buffer.Add(new OpConstant<sbyte>(GetOrRegister(lit.Type), Bound++, (sbyte)lit.IntValue)),
                { Size: <= 16, Signed: false } => Buffer.Add(new OpConstant<ushort>(GetOrRegister(lit.Type), Bound++, (ushort)lit.IntValue)),
                { Size: <= 16, Signed: true } => Buffer.Add(new OpConstant<short>(GetOrRegister(lit.Type), Bound++, (short)lit.IntValue)),
                { Size: <= 32, Signed: false } => Buffer.Add(new OpConstant<uint>(GetOrRegister(lit.Type), Bound++, unchecked((uint)lit.IntValue))),
                { Size: <= 32, Signed: true } => Buffer.Add(new OpConstant<int>(GetOrRegister(lit.Type), Bound++, lit.IntValue)),
                { Size: <= 64, Signed: false } => Buffer.Add(new OpConstant<ulong>(GetOrRegister(lit.Type), Bound++, unchecked((uint)lit.LongValue))),
                { Size: <= 64, Signed: true } => Buffer.Add(new OpConstant<long>(GetOrRegister(lit.Type), Bound++, lit.LongValue)),
                _ => throw new NotImplementedException()
            },
            FloatLiteral lit => lit.Suffix.Size switch
            {
                > 32 => Buffer.Add(new OpConstant<double>(GetOrRegister(lit.Type), Bound++, lit.DoubleValue)),
                _ => Buffer.Add(new OpConstant<float>(GetOrRegister(lit.Type), Bound++, (float)lit.DoubleValue)),
            },
            _ => throw new NotImplementedException()
        };

        result = new(instruction);
        LiteralConstants.Add((literal.Type, literalValue), result);
        AddName(result.Id, literal switch
        {
            BoolLiteral lit => $"{lit.Type}_{lit.Value}",
            IntegerLiteral lit => $"{lit.Type}_{lit.Value}",
            FloatLiteral lit => $"{lit.Type}_{lit.Value}",
            _ => throw new NotImplementedException()
        });
        return result;
    }

    public OpData Insert<T>(int index, in T value)
        where T : struct, IMemoryInstruction, allows ref struct
        => Buffer.InsertData(index, value);

    public OpData Add<T>(in T value)
        where T : struct, IMemoryInstruction, allows ref struct
        => Buffer.Add(value);

    public OpDataIndex Add(OpData data)
        => Buffer.Add(data);


    public SpirvContext FluentAdd<T>(in T value, out T result)
        where T : struct, IMemoryInstruction, allows ref struct
    {
        Buffer.FluentAdd(value, out result);
        return this;
    }

    public void Sort() => Buffer.Sort();

    public int InsertWithoutDuplicates(int? desiredResultId, NewSpirvBuffer source)
    {
        var index = Buffer.Count;
        return InsertWithoutDuplicates(ref index, desiredResultId, source);
    }

    public int InsertWithoutDuplicates(ref int instructionIndex, int? desiredResultId, NewSpirvBuffer source)
    {
        // Import in current buffer (without duplicate)
        var typeDuplicateInserter = new TypeDuplicateHelper(Buffer);
        var remapIds = new Dictionary<int, int>();
        int lastResultId = -1;
        for (int index = 0; index < source.Count; ++index)
        {
            var i = source[index];
            SpirvBuilder.RemapIds(remapIds, ref i.Data);

            //// If it's a generic reference, remap to OpSDSLGenericParameter which has to match during typeDuplicateInserter.CheckForDuplicates()
            var isGenericReference = i.Op == Op.OpSDSLGenericReference;
            if (isGenericReference)
                i.Data.Memory.Span[0] = (int)(i.Data.Memory.Span[0] & 0xFFFF0000) | (int)Op.OpSDSLGenericParameter;

            // Note: we also try to avoid duplciate for constants (which should have been resolved)
            // otherwise a generic type might have 2 different instantiation with same parameters
            if ((TypeDuplicateHelper.OpCheckDuplicateForTypesAndImport(i.Op) || TypeDuplicateHelper.OpCheckDuplicateForConstant(i.Op) || isGenericReference)
                && typeDuplicateInserter.CheckForDuplicates(i.Data, out var existingData))
            {
                // Make sure this data is declared at current index, otherwise move it.
                // Note: it should be safe to do so as the source buffer has all the dependencies and they should have been inserted in previous loops
                if (existingData.Index > instructionIndex)
                {
                    var existingDataCopy = new OpData(existingData.Data.Memory);
                    typeDuplicateInserter.RemoveInstructionAt(existingData.Index, false);
                    existingData = typeDuplicateInserter.InsertInstruction(instructionIndex++, existingDataCopy);
                }
                remapIds.Add(i.Data.IdResult.Value, existingData.Data.IdResult.Value);
                lastResultId = existingData.Data.IdResult.Value;
            }
            else
            {
                if (isGenericReference)
                    i.Data.Memory.Span[0] = (int)(i.Data.Memory.Span[0] & 0xFFFF0000) | (int)Op.OpSDSLGenericReference;

                if (i.Data.IdResult.HasValue)
                {
                    // Make sure to remap last instruction (which we assume is the actual constant) with the desired result ID
                    var resultId = index == source.Count - 1 && desiredResultId != null
                        ? desiredResultId.Value
                        : bound++;

                    remapIds.Add(i.Data.IdResult.Value, resultId);
                    i.Data.IdResult = resultId;
                    typeDuplicateInserter.InsertInstruction(instructionIndex++, i.Data);

                    lastResultId = resultId;
                }
            }
        }

        if (lastResultId == -1)
            throw new InvalidOperationException("Could not find any instruction with a value");

        if (desiredResultId != null && lastResultId != desiredResultId)
        {
            // Need to remap all existing references
            SpirvBuilder.RemapIds(Buffer, 0, Buffer.Count, new Dictionary<int, int> { { lastResultId, desiredResultId.Value } });
        }

        return lastResultId;
    }

    public NewSpirvBuffer ExtractConstantAsSpirvBuffer(int constantId)
    {
        // First, run a simplification pass
        // TODO: separate simplification from computing value?
        SpirvBuilder.TryGetConstantValue(constantId, out _, out _, Buffer, true);

        // Go backward and find any reference
        var newBuffer = new NewSpirvBuffer();
        var referenced = new HashSet<int> { constantId };
        var instructions = new List<OpData>();
        for (int index = Buffer.Count - 1; index >= 0; --index)
        {
            var i = Buffer[index];
            if (i.Data.IdResult is int resultId && referenced.Remove(resultId))
            {
                var i2 = new OpData(i.Data.Memory.Span);

                // Then add IdRef operands to next requested instructions or types
                foreach (var op in i2)
                {
                    if (op.Kind == OperandKind.IdRef
                        || op.Kind == OperandKind.IdResultType
                        || op.Kind == OperandKind.PairIdRefIdRef)
                    {
                        foreach (ref var word in op.Words)
                        {
                            referenced.Add(word);
                        }
                    }
                    else if (op.Kind == OperandKind.PairLiteralIntegerIdRef
                        || op.Kind == OperandKind.PairIdRefLiteralInteger)
                    {
                        throw new NotImplementedException();
                    }
                }

                instructions.Add(i2);
            }
        }

        // Since we went backward, reverse the list
        instructions.Reverse();
        foreach (var i in instructions)
            newBuffer.Add(i);
        return newBuffer;
    }

    [Obsolete("Use the insert method instead")]
    public NewSpirvBuffer GetBuffer() => Buffer;

    public NewSpirvBuffer.Enumerator GetEnumerator() => Buffer.GetEnumerator();

    public override string ToString()
    {
        return Spv.Dis(Buffer, writeToConsole: false);
    }
}
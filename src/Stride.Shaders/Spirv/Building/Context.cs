using CommunityToolkit.HighPerformance;
using Stride.Shaders.Core;
using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Tools;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Building;

public interface IExternalShaderLoader
{
    public void RegisterShader(string name, ReadOnlySpan<ShaderMacro> defines, NewSpirvBuffer buffer);
    public bool Exists(string name);
    public bool LoadExternalBuffer(string name, ReadOnlySpan<ShaderMacro> defines, [MaybeNullWhen(false)] out NewSpirvBuffer bytecode, out bool isFromCache);
}

public abstract class ShaderLoaderBase : IExternalShaderLoader
{
    private Dictionary<string, NewSpirvBuffer> loadedShaders = [];

    public void RegisterShader(string name, ReadOnlySpan<ShaderMacro> defines, NewSpirvBuffer buffer)
    {
        loadedShaders.Add(name, buffer);
    }

    public abstract bool Exists(string name);
    public abstract bool LoadExternalFile(string name, ReadOnlySpan<ShaderMacro> defines, [MaybeNullWhen(false)] out NewSpirvBuffer buffer);

    public bool LoadExternalBuffer(string name, ReadOnlySpan<ShaderMacro> defines, [MaybeNullWhen(false)] out NewSpirvBuffer buffer, out bool isFromCache)
    {
        if (loadedShaders.TryGetValue(name, out buffer))
        {
            isFromCache = true;
            return true;
        }

        isFromCache = false;
        if (!LoadExternalFile(name, defines, out buffer))
        {
            throw new InvalidOperationException($"Shader {name} could not be found");
        }

        return true;
    }
}

// Should contain internal data not seen by the client but helpful for the generation like type symbols and other 
// SPIR-V parameters
public class SpirvContext
{
    public int Bound { get; set; } = 1;
    public Dictionary<SymbolType, int> Types { get; } = [];
    public Dictionary<int, SymbolType> ReverseTypes { get; } = [];
    public Dictionary<(SymbolType Type, object Value), SpirvValue> LiteralConstants { get; } = [];
    NewSpirvBuffer Buffer { get; set; } = new();

    public int? GLSLSet { get; private set; }

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
        => Buffer.Add(new OpName(target, name));

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

    public void AddGlobalVariable(Symbol variable)
    {
        throw new NotImplementedException();
        // var t = GetOrRegister(variable.Type);
        // if (variable.Id.Storage == Storage.Stream)
        // {
        //     foreach(var usage in SymbolProvider.RootSymbols.StreamUsages[variable.Id])
        //     {
        //         var i = Buffer.AddOpVariable(
        //             Bound++, 
        //             t, 
        //             usage.IO switch
        //             {
        //                 StreamIO.Input => StorageClass.Input,
        //                 StreamIO.Output => StorageClass.Output,
        //                 _ => throw new NotImplementedException()
        //             }, 
        //             null
        //         );
        //         Variables[variable.Id.Name] = i.ResultId!.Value;
        //         AddName(i, $"{usage.EntryPoint.ToString()}_{usage.IO.ToString()}_{variable.Id.Name}");
        //     }
        // }
        // else
        // {
        //     var storage = variable.Id.Storage switch
        //     {
        //         Storage.UniformConstant => StorageClass.UniformConstant,
        //         Storage.Uniform => StorageClass.Uniform,
        //         Storage.Function => StorageClass.Function,
        //         Storage.Generic => StorageClass.Generic,
        //         _ => throw new NotImplementedException("Variable has to have a storage class")
        //     };
        //     var i = Buffer.AddOpVariable(Bound++, t, storage, null);
        //     Variables[variable.Id.Name] = i.ResultId!.Value;
        //     AddName(i, $"{variable.Id.Name}");
        // }
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
                ArrayType a when a.Size != -1 || a.SizeExpressionId != null => RegisterArrayType(a),
                ArrayType a when a.Size == -1 && a.SizeExpressionId == null => Buffer.Add(new OpTypeRuntimeArray(Bound++, GetOrRegister(a.BaseType))).IdResult,
                StructType st => RegisterStructuredType(st.ToId(), st),
                FunctionType f => RegisterFunctionType(f),
                PointerType p => RegisterPointerType(p),
                LoadedShaderSymbol s => ImportShaderType(s),
                Texture1DType t => Buffer.Add(new OpTypeImage(Bound++, GetOrRegister(t.ReturnType), t.Dimension, t.Depth, t.Arrayed ? 1 : 0, t.Multisampled ? 1 : 0, t.Sampled, t.Format, null)).IdResult,
                Texture2DType t => Buffer.Add(new OpTypeImage(Bound++, GetOrRegister(t.ReturnType), t.Dimension, t.Depth, t.Arrayed ? 1 : 0, t.Multisampled ? 1 : 0, t.Sampled, t.Format, null)).IdResult,
                Texture3DType t => Buffer.Add(new OpTypeImage(Bound++, GetOrRegister(t.ReturnType), t.Dimension, t.Depth, t.Arrayed ? 1 : 0, t.Multisampled ? 1 : 0, t.Sampled, t.Format, null)).IdResult,
                SamplerType st => Buffer.Add(new OpTypeSampler(Bound++)).IdResult,
                BufferType b => Buffer.Add(new OpTypeImage(Bound++, GetOrRegister(b.BaseType), Dim.Buffer, 2, 0, 0, 1, ImageFormat.Unknown, null)).IdResult,
                SampledImage si => Buffer.Add(new OpTypeSampledImage(Bound++, GetOrRegister(si.ImageType))).IdResult,
                GenericLinkType => Buffer.Add(new OpTypeGenericLinkSDSL(Bound++)).IdResult,
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
        var sizeId = a.Size != -1
            ? CompileConstant((int)a.Size).Id
            : a.SizeExpressionId ?? throw new InvalidOperationException();

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
            FluentAdd(new OpSDSLImportStruct(Bound++, structType.Type.ToId(), shader.ResultId), out var @struct);
            AddName(@struct.ResultId, structType.Type.Name);
            // Fill the ID
            structType.ImportedId = @struct.ResultId;

            // Register it so that it can be used right after during OpVariable for cbuffer
            Types.Add(structType.Type, structType.ImportedId);
            ReverseTypes.Add(structType.ImportedId, structType.Type);
        }

        // Import variables/functions
        var methods = CollectionsMarshal.AsSpan(shaderSymbol.Methods);
        foreach (ref var c in methods)
        {
            if (c.Symbol.Id.Kind == SymbolKind.Method)
            {
                var functionType = (FunctionType)c.Symbol.Type;
                var functionReturnTypeId = GetOrRegister(functionType.ReturnType);

                c.Symbol.IdRef = Bound++;
                Add(new OpSDSLImportFunction(c.Symbol.IdRef, functionReturnTypeId, c.Symbol.Id.Name, shader.ResultId, c.Flags));
                AddName(c.Symbol.IdRef, c.Symbol.Id.Name);
            }
        }
        var variables = CollectionsMarshal.AsSpan(shaderSymbol.Variables);
        foreach (ref var c in variables)
        {
            if (c.Symbol.Id.Kind == SymbolKind.Variable)
            {
                c.Symbol.IdRef = Bound++;
                Add(new OpSDSLImportVariable(c.Symbol.IdRef, GetOrRegister(c.Symbol.Type), c.Symbol.Id.Name, shader.ResultId, c.Flags));
                AddName(c.Symbol.IdRef, c.Symbol.Id.Name);
            }
        }

        return shader.ResultId;
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

            if (member.Type is MatrixType)
            {
                if (member.TypeModifier != TypeModifier.ColumnMajor)
                    Add(new OpMemberDecorate(id, index, new ParameterizedFlag<Decoration>(Decoration.ColMajor, [])));
                else if (member.TypeModifier != TypeModifier.RowMajor)
                    Add(new OpMemberDecorate(id, index, new ParameterizedFlag<Decoration>(Decoration.RowMajor, [])));
            }

        }

        Types[structSymbol] = id;
        ReverseTypes[id] = structSymbol;

        return id;
    }


    int RegisterFunctionType(FunctionType functionType)
    {
        Span<int> types = stackalloc int[functionType.ParameterTypes.Count];
        for (int i = 0; i < functionType.ParameterTypes.Count; i++)
            types[i] = GetOrRegister(functionType.ParameterTypes[i]);

        var result = Buffer.Add(new OpTypeFunction(Bound++, GetOrRegister(functionType.ReturnType), [.. types]));
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
        where T : struct, IMemoryInstruction
        => Buffer.InsertData(index, value);

    public OpData Add<T>(in T value)
        where T : struct, IMemoryInstruction
        => Buffer.Add(value);


    public SpirvContext FluentAdd<T>(in T value, out T result)
        where T : struct, IMemoryInstruction
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
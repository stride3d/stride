using Stride.Shaders.Core;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Tools;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Building;

public interface IExternalShaderLoader
{
    public bool LoadExternalReference(string name, [MaybeNullWhen(false)] out byte[] bytecode);
}

// Should contain internal data not seen by the client but helpful for the generation like type symbols and other 
// SPIR-V parameters
public class SpirvContext(SpirvModule module)
{
    public int Bound { get; set; } = 1;
    public string? Name { get; private set; }
    public SpirvModule Module { get; } = module;
    public SortedList<string, SpirvValue> Variables { get; } = [];
    public Dictionary<SymbolType, int> Types { get; } = [];
    public Dictionary<int, SymbolType> ReverseTypes { get; } = [];
    public Dictionary<(SymbolType Type, object Value), SpirvValue> LiteralConstants { get; } = [];
    public NewSpirvBuffer Buffer { get; set; } = new();

    public void PutShaderName(string name)
    {
        if (Name is null)
        {
            Name = name;
            Buffer.Insert(0, new OpSDSLShader(name));
        }
        else throw new NotImplementedException();
    }

    public void AddName(int target, string name)
        => Buffer.Add(new OpName(target, name.Replace('.', '_')));

    public void AddMemberName(int target, int accessor, string name)
        => Buffer.Add(new OpMemberName(target, accessor, name.Replace('.', '_')));

    public int AddConstant<TScalar>(TScalar value)
        where TScalar : INumber<TScalar>
    {
        var data = value switch
        {
            byte v => Buffer.Add(new OpConstant<byte>(Bound++, GetOrRegister(ScalarType.From("byte")), v)),
            sbyte v => Buffer.Add(new OpConstant<sbyte>(Bound++, GetOrRegister(ScalarType.From("sbyte")), v)),
            ushort v => Buffer.Add(new OpConstant<ushort>(Bound++, GetOrRegister(ScalarType.From("ushort")), v)),
            short v => Buffer.Add(new OpConstant<short>(Bound++, GetOrRegister(ScalarType.From("short")), v)),
            uint v => Buffer.Add(new OpConstant<uint>(Bound++, GetOrRegister(ScalarType.From("uint")), v)),
            int v => Buffer.Add(new OpConstant<int>(Bound++, GetOrRegister(ScalarType.From("int")), v)),
            ulong v => Buffer.Add(new OpConstant<ulong>(Bound++, GetOrRegister(ScalarType.From("ulong")), v)),
            long v => Buffer.Add(new OpConstant<long>(Bound++, GetOrRegister(ScalarType.From("long")), v)),
            Half v => Buffer.Add(new OpConstant<Half>(Bound++, GetOrRegister(ScalarType.From("half")), v)),
            float v => Buffer.Add(new OpConstant<float>(Bound++, GetOrRegister(ScalarType.From("float")), v)),
            double v => Buffer.Add(new OpConstant<double>(Bound++, GetOrRegister(ScalarType.From("bdouble")), v)),
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
            pvariables[pos++] = Variables[v.Id.Name].Id;
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
                MatrixType m => Buffer.Add(new OpTypeVector(Bound++, GetOrRegister(new VectorType(m.BaseType, m.Rows)), m.Columns)).IdResult,
                ArrayType a => Buffer.Add(new OpTypeArray(Bound++, GetOrRegister(a.BaseType), a.Size)).IdResult,
                StructType st => RegisterStructuredType(st.ToId(), st),
                ConstantBufferSymbol cb => RegisterCBuffer(cb),
                FunctionType f => RegisterFunctionType(f),
                PointerType p => RegisterPointerType(p),
                // TextureSymbol t => Buffer.AddOpTypeImage(Bound++, Register(t.BaseType), t.),
                // StructSymbol st => RegisterStruct(st),
                _ => throw new NotImplementedException($"Can't add type {type}")
            };
            Types[type] = instruction;
            ReverseTypes[instruction] = type;
            return instruction;
        }
    }

    private int RegisterCBuffer(ConstantBufferSymbol cb)
    {
        var result = RegisterStructuredType($"type.{cb.ToId()}", cb);

        Buffer.Add(new OpDecorate(result, Decoration.Block));
        for (var index = 0; index < cb.Members.Count; index++)
        {
            if (index > 0)
                throw new NotImplementedException("Offset");
            Buffer.Add(new OpMemberDecorate(result, index, Decoration.Offset, 0));
        }

        return result;
    }

    int RegisterStructuredType(string name, StructuredType structSymbol)
    {
        Span<int> types = stackalloc int[structSymbol.Members.Count];
        for (var index = 0; index < structSymbol.Members.Count; index++)
            types[index] = GetOrRegister(structSymbol.Members[index].Type);

        var result = Buffer.Add(new OpTypeStruct(Bound++, [.. types]));
        var id = result.IdResult;
        AddName(id, name);
        for (var index = 0; index < structSymbol.Members.Count; index++)
            AddMemberName(id, index, structSymbol.Members[index].Name);
        return id;
    }


    int RegisterFunctionType(FunctionType functionType)
    {
        Span<int> types = stackalloc int[functionType.ParameterTypes.Count];
        int tmp = 0;
        foreach (var f in functionType.ParameterTypes)
            types[tmp] = GetOrRegister(f);
        var result = Buffer.Add(new OpTypeFunction(Bound++, GetOrRegister(functionType.ReturnType), [.. types]));
        // disabled for now: currently it generates name with {}, not working with most SPIRV tools
        // AddName(result, functionType.ToId());
        return result.IdResult;
    }

    int RegisterPointerType(PointerType pointerType)
    {
        var baseType = GetOrRegister(pointerType.BaseType);
        var result = Buffer.Add(new OpTypePointer(Bound++, pointerType.StorageClass, baseType));
        var id = result.IdResult;
        AddName(id, pointerType.ToId());
        return id;
    }

    public SpirvValue CreateConstant(Literal literal)
    {
        // object literalValue = literal switch
        // {
        //     BoolLiteral lit => lit.Value,
        //     IntegerLiteral lit => lit.Suffix.Size switch
        //     {
        //         > 32 => lit.LongValue,
        //         _ => lit.IntValue,
        //     },
        //     FloatLiteral lit => lit.Suffix.Size switch
        //     {
        //         > 32 => lit.DoubleValue,
        //         _ => (float)lit.DoubleValue,
        //     },
        // };

        // if (LiteralConstants.TryGetValue((literal.Type, literalValue), out var result))
        //     return result;
        var instruction = literal switch
        {
            BoolLiteral { Value: true } lit => Buffer.Add(new OpConstantTrue(Bound++, GetOrRegister(lit.Type))),
            BoolLiteral { Value: false } lit => Buffer.Add(new OpConstantFalse(Bound++, GetOrRegister(lit.Type))),
            IntegerLiteral lit => lit.Suffix switch
            {
                { Size: <= 8, Signed: false } => Buffer.Add(new OpConstant<byte>(Bound++, GetOrRegister(lit.Type), (byte)lit.IntValue)),
                { Size: <= 8, Signed: true } => Buffer.Add(new OpConstant<sbyte>(Bound++, GetOrRegister(lit.Type), (sbyte)lit.IntValue)),
                { Size: <= 16, Signed: false } => Buffer.Add(new OpConstant<ushort>(Bound++, GetOrRegister(lit.Type), (ushort)lit.IntValue)),
                { Size: <= 16, Signed: true } => Buffer.Add(new OpConstant<short>(Bound++, GetOrRegister(lit.Type), (short)lit.IntValue)),
                { Size: <= 32, Signed: false } => Buffer.Add(new OpConstant<uint>(Bound++, GetOrRegister(lit.Type), unchecked((uint)lit.IntValue))),
                { Size: <= 32, Signed: true } => Buffer.Add(new OpConstant<int>(Bound++, GetOrRegister(lit.Type), lit.IntValue)),
                _ => throw new NotImplementedException()
            },
            FloatLiteral lit => lit.Suffix.Size switch
            {
                > 32 => Buffer.Add(new OpConstant<double>(Bound++, GetOrRegister(lit.Type), lit.DoubleValue)),
                _ => Buffer.Add(new OpConstant<float>(Bound++, GetOrRegister(lit.Type), (float)lit.DoubleValue)),
            },
            _ => throw new NotImplementedException()
        };

        SpirvValue result = new(instruction);
        // LiteralConstants.Add((literal.Type, literalValue), result);
        AddName(result.Id, literal switch
        {
            BoolLiteral lit => $"{lit.Type}_{lit.Value}",
            IntegerLiteral lit => $"{lit.Type}_{lit.Value}",
            FloatLiteral lit => $"{lit.Type}_{lit.Value}",
            _ => throw new NotImplementedException()
        });
        return result;
    }

    public override string ToString()
    {
        return Spv.Dis(Buffer);
    }
}
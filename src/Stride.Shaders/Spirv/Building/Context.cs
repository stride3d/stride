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
    public int Bound { get; internal set; } = 1;
    public string? Name { get; private set; }
    public SpirvModule Module { get; } = module;
    public SortedList<string, SpirvValue> Variables { get; } = [];
    public Dictionary<SymbolType, int> Types { get; } = [];
    public Dictionary<int, SymbolType> ReverseTypes { get; } = [];
    public Dictionary<(SymbolType Type, object Value), SpirvValue> LiteralConstants { get; } = [];
    public SpirvBuffer Buffer { get; set; } = new();

    public void PutShaderName(string name)
    {
        if (Name is null)
        {
            Name = name;
            Buffer.InsertOpSDSLShader(0, name);
        }
        else throw new NotImplementedException();
    }

    public void AddName(IdRef target, string name)
        => Buffer.AddOpName(target, name);

    public void AddMemberName(IdRef target, int accessor, string name)
        => Buffer.AddOpMemberName(target, accessor, name);

    public IdRef AddConstant<TScalar>(TScalar value)
        where TScalar : INumber<TScalar>
    {
        return value switch
        {
            byte v => Buffer.AddOpConstant<LiteralInteger>(Bound++, GetOrRegister(ScalarType.From("byte")), v),
            sbyte v => Buffer.AddOpConstant<LiteralInteger>(Bound++, GetOrRegister(ScalarType.From("sbyte")), v),
            ushort v => Buffer.AddOpConstant<LiteralInteger>(Bound++, GetOrRegister(ScalarType.From("ushort")), v),
            short v => Buffer.AddOpConstant<LiteralInteger>(Bound++, GetOrRegister(ScalarType.From("short")), v),
            uint v => Buffer.AddOpConstant<LiteralInteger>(Bound++, GetOrRegister(ScalarType.From("uint")), v),
            int v => Buffer.AddOpConstant<LiteralInteger>(Bound++, GetOrRegister(ScalarType.From("int")), v),
            ulong v => Buffer.AddOpConstant<LiteralInteger>(Bound++, GetOrRegister(ScalarType.From("ulong")), v),
            long v => Buffer.AddOpConstant<LiteralInteger>(Bound++, GetOrRegister(ScalarType.From("long")), v),
            Half v => Buffer.AddOpConstant<LiteralFloat>(Bound++, GetOrRegister(ScalarType.From("half")), v),
            float v => Buffer.AddOpConstant<LiteralFloat>(Bound++, GetOrRegister(ScalarType.From("float")), v),
            double v => Buffer.AddOpConstant<LiteralFloat>(Bound++, GetOrRegister(ScalarType.From("bdouble")), v),
            _ => throw new NotImplementedException()
        };
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


    public void SetEntryPoint(ExecutionModel model, IdRef function, string name, ReadOnlySpan<Symbol> variables)
    {
        Span<IdRef> pvariables = stackalloc IdRef[variables.Length];
        int pos = 0;
        foreach (var v in variables)
            pvariables[pos++] = Variables[v.Id.Name].Id;
        Buffer.AddOpEntryPoint(model, function, name, pvariables);
    }

    public IdRef GetOrRegister(SymbolType? type)
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
                        "void" => Buffer.AddOpTypeVoid(Bound++),
                        "bool" => Buffer.AddOpTypeBool(Bound++),
                        "sbyte" => Buffer.AddOpTypeInt(Bound++, 8, 1),
                        "byte" => Buffer.AddOpTypeInt(Bound++, 8, 0),
                        "ushort" => Buffer.AddOpTypeInt(Bound++, 16, 1),
                        "short" => Buffer.AddOpTypeInt(Bound++, 16, 0),
                        "int" => Buffer.AddOpTypeInt(Bound++, 32, 1),
                        "uint" => Buffer.AddOpTypeInt(Bound++, 32, 0),
                        "long" => Buffer.AddOpTypeInt(Bound++, 64, 1),
                        "ulong" => Buffer.AddOpTypeInt(Bound++, 64, 0),
                        "half" => Buffer.AddOpTypeFloat(Bound++, 16, null),
                        "float" => Buffer.AddOpTypeFloat(Bound++, 32, null),
                        "double" => Buffer.AddOpTypeFloat(Bound++, 64, null),
                        _ => throw new NotImplementedException($"Can't add type {type}")

                    },
                VectorType v => Buffer.AddOpTypeVector(Bound++, GetOrRegister(v.BaseType), v.Size),
                MatrixType m => Buffer.AddOpTypeVector(Bound++, GetOrRegister(new VectorType(m.BaseType, m.Rows)), m.Columns),
                ArrayType a => Buffer.AddOpTypeArray(Bound++, GetOrRegister(a.BaseType), a.Size),
                StructType st => RegisterStruct(st),
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

    IdRef RegisterStruct(StructType structSymbol)
    {
        Span<IdRef> types = stackalloc IdRef[structSymbol.Fields.Count];
        for (var index = 0; index < structSymbol.Fields.Count; index++)
            types[index] = GetOrRegister(structSymbol.Fields[index].Type);

        var result = Buffer.AddOpTypeStruct(Bound++, types);
        AddName(result, structSymbol.ToId());
        for (var index = 0; index < structSymbol.Fields.Count; index++)
            AddMemberName(result, index, structSymbol.Fields[index].Name);

        return result;
    }

    IdRef RegisterFunctionType(FunctionType functionType)
    {
        Span<IdRef> types = stackalloc IdRef[functionType.ParameterTypes.Count];
        int tmp = 0;
        foreach (var f in functionType.ParameterTypes)
            types[tmp] = GetOrRegister(f);
        var result = Buffer.AddOpTypeFunction(Bound++, GetOrRegister(functionType.ReturnType), types);
        // disabled for now: currently it generates name with {}, not working with most SPIRV tools
        AddName(result, functionType.ToId());
        return result;
    }

    IdRef RegisterPointerType(PointerType pointerType)
    {
        var baseType = GetOrRegister(pointerType.BaseType);
        var result = Buffer.AddOpTypePointer(Bound++, Specification.StorageClass.Function, baseType);
        AddName(result, pointerType.ToId());
        return result;
    }

    public SpirvValue CreateConstant(Literal literal)
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

        if (LiteralConstants.TryGetValue((literal.Type, literalValue), out var result))
            return result;

        var instruction = literal switch
        {
            BoolLiteral lit => lit.Value switch
            {
                true => Buffer.AddOpConstantTrue(Bound++, GetOrRegister(lit.Type)),
                false => Buffer.AddOpConstantFalse(Bound++, GetOrRegister(lit.Type))
            },
            IntegerLiteral lit => lit.Suffix.Size switch
            {
                > 32 => Buffer.AddOpConstant<LiteralInteger>(Bound++, GetOrRegister(lit.Type), lit.LongValue),
                _ => Buffer.AddOpConstant<LiteralInteger>(Bound++, GetOrRegister(lit.Type), lit.IntValue),
            },
            FloatLiteral lit => lit.Suffix.Size switch
            {
                > 32 => Buffer.AddOpConstant<LiteralFloat>(Bound++, GetOrRegister(lit.Type), lit.DoubleValue),
                _ => Buffer.AddOpConstant<LiteralFloat>(Bound++, GetOrRegister(lit.Type), (float)lit.DoubleValue),
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
            });
        return result;
    }

    public override string ToString()
    {
        return new SpirvDis<SpirvBuffer>(Buffer).Disassemble();
    }
}
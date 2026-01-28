using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Stride.Shaders.Core;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Core.Parsing;
using Stride.Shaders.Spirv.Tools;
using Stride.Shaders.Spirv.Building;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Experiments;


public static partial class Examples
{
    public static void GenerateSpirv()
    {
        var compiler = new CompilerUnit();
        var (builder, context) = compiler;
        var table = new SymbolTable(context);

        context.GetOrRegister(new MatrixType(ScalarType.Float, 4, 3));
        context.GetOrRegister(ScalarType.Int);


        // context.AddGlobalVariable(new(new("color", SymbolKind.Variable, Storage.Stream), VectorType.From("float4")));

        var function = builder.DeclareFunction(
            context,
            "add",
            new(ScalarType.Int, [new(ScalarType.Int, default), new(ScalarType.Int, default)])
        );
        builder.BeginFunction(context, function);
        builder.AddFunctionParameter(context, "a", ScalarType.Int);
        builder.AddFunctionParameter(context, "b", ScalarType.Int);
        builder.SetPositionTo(function);
        var block = builder.CreateBlock(context, "sourceBlock");
        builder.SetPositionTo(block);
        var v = builder.BinaryOperation(
            table,
            context,
            function.Parameters["a"], Operator.Plus, function.Parameters["b"],
            default
        );
        builder.Return(v);
        builder.EndFunction();
        context.Sort();
        Spv.Dis(compiler.ToBuffer());
    }

    public static void ParseShader()
    {
        Console.WriteLine(Unsafe.SizeOf<Memory<int>>());

        InstructionInfo.GetInfo(Op.OpCapability);

        var shader = File.ReadAllBytes("../../shader.spv");
    }

    public static void CreateNewShader()
    {
        int id = 1;

        // var bound = new Bound();
        var buffer = new NewSpirvBuffer();
        // Capabilities


        buffer.FluentAdd(new OpCapability(Capability.Shader));
        var extInstImport = new OpExtInstImport(id++, "GLSL.std.450");
        buffer.Add(extInstImport);
        buffer.FluentAdd(new OpMemoryModel(AddressingModel.Logical, MemoryModel.GLSL450));


        // declarations

        buffer.FluentAdd(new OpTypeVoid(id++), out var t_void);

        buffer.FluentAdd(new OpTypeBool(id++), out var t_bool);
        buffer.FluentAdd(new OpTypeFunction(t_void, id++, []), out var t_func);
        buffer.FluentAdd(new OpTypeFloat(id++, 32, null), out var t_float);
        buffer.FluentAdd(new OpTypeInt(id++, 32, 0), out var t_uint);
        buffer.FluentAdd(new OpTypeInt(id++, 32, 1), out var t_int);
        buffer.FluentAdd(new OpTypeFunction(id++, returnType: t_int, [t_int, t_int]), out var t_func_add);
        buffer.FluentAdd(new OpTypeVector(id++, componentType: t_float, 4), out var t_float4);
        buffer.FluentAdd(new OpTypePointer(id++, StorageClass.Function, t_float4), out var t_p_float4_func);
        buffer.FluentAdd(new OpConstant<float>(t_float, id++, 5f), out var constant1);
        buffer.FluentAdd(new OpConstant<float>(t_float, id++, 2.23f), out var constant2);
        buffer.FluentAdd(new OpConstant<int>(t_uint, id++, 5), out var constant3);
        buffer.FluentAdd(new OpConstantComposite(
            t_float4,
            id++,
            [constant1, constant1, constant2, constant1]
        ), out var compositeType);
        buffer.FluentAdd(new OpTypeArray(t_float4, id++, constant3), out var t_array);
        buffer.FluentAdd(new OpTypeStruct(id++, [t_uint, t_array, t_int]), out var t_struct);
        buffer.FluentAdd(new OpTypeStruct(id++, [t_struct, t_uint]), out var t_struct2);
        buffer.FluentAdd(new OpTypePointer(id++, StorageClass.Uniform, t_struct2), out var t_p_struct2);
        buffer.FluentAdd(new OpVariable(t_p_struct2, id++, StorageClass.Uniform, null), out var v_struct2);
        buffer.FluentAdd(new OpConstant<int>(t_int, id++, 1), out var constant4);
        buffer.FluentAdd(new OpTypePointer(id++, StorageClass.Uniform, t_uint), out var t_p_uint);
        buffer.FluentAdd(new OpConstant<int>(t_uint, id++, 0), out var constant5);
        buffer.FluentAdd(new OpTypePointer(id++, StorageClass.Output, t_float4), out var t_p_output);
        buffer.FluentAdd(new OpVariable(t_p_output, id++, StorageClass.Output, null), out var v_output);
        buffer.FluentAdd(new OpTypePointer(id++, StorageClass.Input, t_float4), out var t_p_input);
        buffer.FluentAdd(new OpVariable(t_p_input, id++, StorageClass.Input, null), out var v_input);
        buffer.FluentAdd(new OpConstant<int>(t_int, id++, 0), out var constant6);
        buffer.FluentAdd(new OpConstant<int>(t_int, id++, 2), out var constant7);
        buffer.FluentAdd(new OpTypePointer(id++, StorageClass.Uniform, t_float4), out var t_p_float4_unif);
        buffer.FluentAdd(new OpVariable(t_p_input, id++, StorageClass.Input, null), out var v_input_2);
        buffer.FluentAdd(new OpTypePointer(id++, StorageClass.Function, t_int), out var t_p_func);
        buffer.FluentAdd(new OpConstant<int>(t_int, id++, 4), out var constant8);
        buffer.FluentAdd(new OpVariable(t_p_input, id++, StorageClass.Input, null), out var v_input_3);
        buffer.FluentAdd(new OpDecorate(t_array, Decoration.ArrayStride, [16]));
        buffer.FluentAdd(new OpMemberDecorate(t_struct, 0, Decoration.Offset, [0]));
        buffer.FluentAdd(new OpMemberDecorate(t_struct, 1, Decoration.Offset, [16]));
        buffer.FluentAdd(new OpMemberDecorate(t_struct, 2, Decoration.Offset, [96]));
        buffer.FluentAdd(new OpMemberDecorate(t_struct2, 0, Decoration.Offset, [0]));
        buffer.FluentAdd(new OpMemberDecorate(t_struct2, 1, Decoration.Offset, [112]));
        buffer.FluentAdd(new OpDecorate(t_struct2, Decoration.Block, []));
        buffer.FluentAdd(new OpDecorate(v_struct2, Decoration.DescriptorSet, [0]));
        buffer.FluentAdd(new OpDecorate(v_input_2, Decoration.NoPerspective, []));
        buffer.FluentAdd(new OpName(t_p_func, "main"));
        buffer.FluentAdd(new OpName(t_struct, "S"));
        buffer.FluentAdd(new OpMemberName(t_struct, 0, "b"));
        buffer.FluentAdd(new OpMemberName(t_struct, 1, "v"));
        buffer.FluentAdd(new OpMemberName(t_struct, 2, "i"));
        buffer.FluentAdd(new OpFunction(t_int, id++, FunctionControlMask.None, t_func_add), out var add);


        buffer.FluentAdd(new OpFunctionParameter(t_int, id++), out var a);
        buffer.FluentAdd(new OpFunctionParameter(t_int, id++), out var b);
        buffer.FluentAdd(new OpLabel(id++), out var label);
        buffer.FluentAdd(new OpIAdd(t_int, id++, a, b), out var res);
        buffer.FluentAdd(new OpReturnValue(res));
        buffer.FluentAdd(new OpFunctionEnd());
        buffer.FluentAdd(new OpFunction(t_void, id++, FunctionControlMask.None, t_func), out var main);
        buffer.FluentAdd(new OpEntryPoint(ExecutionModel.Fragment, main, "PSMain", [v_output, v_input, v_input_2, v_input_3]));
        buffer.FluentAdd(new OpExecutionMode(main, ExecutionMode.OriginLowerLeft, []));
        buffer.FluentAdd(new OpLabel(id++), out var label2);
        buffer.FluentAdd(new OpFunctionCall(t_int, id++, add, [constant7, constant7]), out var resAdd);
        buffer.FluentAdd(new OpReturn());
        buffer.FluentAdd(new OpFunctionEnd());

        buffer.Sort();
        var bytecode = buffer.ToBytecode();

        Spv.Dis(buffer);
        File.WriteAllBytes(
            "test.spv",
            bytecode
        );
    }


    public static void CreateShader()
    {
        // int id = 1;

        // // var bound = new Bound();
        // var buffer = new SpirvBuffer();
        // // // Capabilities

        // buffer.AddOpCapability(Capability.Shader);
        // var extInstImport = buffer.AddOpExtInstImport(id++, "GLSL.std.450");
        // buffer.AddOpMemoryModel(AddressingModel.Logical, MemoryModel.GLSL450);


        // // declarations

        // Span<IdRef> c = stackalloc IdRef[10]; // This is for use in parameters


        // var t_void = buffer.AddOpTypeVoid(id++);

        // var t_bool = buffer.AddOpTypeBool(id++);

        // var t_func = buffer.AddOpTypeFunction(id++, t_void, []);
        // var t_float = buffer.AddOpTypeFloat(id++, 32, null);
        // var t_uint = buffer.AddOpTypeInt(id++, 32, 0);
        // var t_int = buffer.AddOpTypeInt(id++, 32, 1);
        // var t_func_add = buffer.AddOpTypeFunction(id++, t_int, [t_int, t_int]);
        // var t_float4 = buffer.AddOpTypeVector(id++, t_float, 4);
        // var t_p_float4_func = buffer.AddOpTypePointer(id++, StorageClass.Function, t_float4);
        // var constant1 = buffer.AddOpConstant<LiteralFloat>(id++, t_float, 5);
        // var constant2 = buffer.AddOpConstant<LiteralFloat>(id++, t_float, 2.23f);
        // var constant3 = buffer.AddOpConstant<LiteralInteger>(id++, t_uint, 5);
        // var compositeType = buffer.AddOpConstantComposite(
        //     id++,
        //     t_float4,
        //     [constant1, constant1, constant2, constant1]
        // );

        // var t_array = buffer.AddOpTypeArray(id++, t_float4, constant3);

        // var t_struct = buffer.AddOpTypeStruct(id++, [t_uint, t_array, t_int]);
        // var t_struct2 = buffer.AddOpTypeStruct(id++, [t_struct, t_uint]);

        // var t_p_struct2 = buffer.AddOpTypePointer(id++, StorageClass.Uniform, t_struct2);

        // var v_struct2 = buffer.AddOpVariable(id++, t_p_struct2, StorageClass.Uniform, null);

        // var constant4 = buffer.AddOpConstant<LiteralInteger>(id++, t_int, 1);

        // var t_p_uint = buffer.AddOpTypePointer(id++, StorageClass.Uniform, t_uint);
        // var constant5 = buffer.AddOpConstant<LiteralInteger>(id++, t_uint, 0);

        // var t_p_output = buffer.AddOpTypePointer(id++, StorageClass.Output, t_float4);
        // var v_output = buffer.AddOpVariable(id++, t_p_output, StorageClass.Output, null);

        // var t_p_input = buffer.AddOpTypePointer(id++, StorageClass.Input, t_float4);
        // var v_input = buffer.AddOpVariable(id++, t_p_input, StorageClass.Input, null);

        // var constant6 = buffer.AddOpConstant<LiteralInteger>(id++, t_int, 0);
        // var constant7 = buffer.AddOpConstant<LiteralInteger>(id++, t_int, 2);
        // var t_p_float4_unif = buffer.AddOpTypePointer(id++, StorageClass.Uniform, t_float4);

        // var v_input_2 = buffer.AddOpVariable(id++, t_p_input, StorageClass.Input, null);
        // var t_p_func = buffer.AddOpTypePointer(id++, StorageClass.Function, t_int);
        // var constant8 = buffer.AddOpConstant<LiteralInteger>(id++, t_int, 4);
        // var v_input_3 = buffer.AddOpVariable(id++, t_p_input, StorageClass.Input, null);





        // buffer.AddOpDecorate(t_array, Decoration.ArrayStride, 16);
        // buffer.AddOpMemberDecorate(t_struct, 0, Decoration.Offset, 0);
        // buffer.AddOpMemberDecorate(t_struct, 1, Decoration.Offset, 16);
        // buffer.AddOpMemberDecorate(t_struct, 2, Decoration.Offset, 96);
        // buffer.AddOpMemberDecorate(t_struct2, 0, Decoration.Offset, 0);
        // buffer.AddOpMemberDecorate(t_struct2, 1, Decoration.Offset, 112);
        // buffer.AddOpDecorate(t_struct2, Decoration.Block);
        // buffer.AddOpDecorate(v_struct2, Decoration.DescriptorSet, 0);
        // buffer.AddOpDecorate(v_input_2, Decoration.NoPerspective);




        // buffer.AddOpName(t_p_func, "main");
        // buffer.AddOpName(t_struct, "S");
        // buffer.AddOpMemberName(t_struct, 0, "b");
        // buffer.AddOpMemberName(t_struct, 1, "v");
        // buffer.AddOpMemberName(t_struct, 2, "i");

        // var add = buffer.AddOpFunction(id++, t_int, FunctionControlMask.None, t_func_add);
        // var a = buffer.AddOpFunctionParameter(id++, t_int);
        // var b = buffer.AddOpFunctionParameter(id++, t_int);
        // buffer.AddOpLabel(id++);
        // var res = buffer.AddOpIAdd(id++, t_int, a, b);
        // buffer.AddOpReturnValue(res);
        // buffer.AddOpFunctionEnd();

        // var main = buffer.AddOpFunction(id++, t_void, FunctionControlMask.None, t_func);
        // buffer.AddOpEntryPoint(ExecutionModel.Fragment, main, "PSMain", [v_output, v_input, v_input_2, v_input_3]);
        // buffer.AddOpExecutionMode(main, ExecutionMode.OriginLowerLeft);

        // buffer.AddOpLabel(id++);
        // var resAdd = buffer.AddOpFunctionCall(id++, t_int, add, [constant7, constant7]);
        // buffer.AddOpReturn();
        // buffer.AddOpFunctionEnd();





        // buffer.Sort();

        // var dis = new SpirvDis<SpirvBuffer>(buffer, useNames: true);

        // dis.Disassemble(writeToConsole: true);
        // File.WriteAllBytes(
        //     "test.spv",
        //     MemoryMarshal.Cast<int, byte>(buffer.ToBuffer().AsSpan())
        // );
#warning replace
        throw new NotImplementedException();
    }


    public static void ParseWorking()
    {
        // var path = @"C:\Users\youness_kafia\Documents\dotnetProjs\SDSLParser\src\Stride.Shaders.Spirv\working1-6.spv";
        var path = @"C:\Users\kafia\source\repos\SDSLParser\src\Stride.Shaders.Spirv\working1-6.spv";

        var bytes = File.ReadAllBytes(path);

        using var bytecode = SpirvBytecode.CreateBufferFromBytecode(bytes);
        var extInst = (OpExtInstImport)bytecode.Buffer[1];
        Console.WriteLine(extInst.Name);
    }
}

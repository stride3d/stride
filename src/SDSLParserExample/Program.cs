using Eto.Parse;
using Eto.Parse.Grammars;
using SDSL.Parsing;
using SDSL.Parsing.Grammars.Expression;
using System.Diagnostics;
using System.Linq;
using Stride.Core.Shaders.Grammar;
using Stride.Core.Shaders;
using Stride.Core.Shaders.Parser;
using Stride.Core.Shaders.Grammar.Stride;
using SDSL.Parsing.AST.Shader;
using SDSL.Mixing;
using SDSL.ThreeAddress;
using SDSL.Parsing.AST.Shader.Analysis;
using System.Text;
using SPIRVCross;
using static SPIRVCross.SPIRV;
using SoftTouch.Spirv.Core;
using static Spv.Specification;
using SDSLParserExample;
using SoftTouch.Spirv;

static void ThreeAddress()
{
    var symb = new SymbolTable();
    var flt = symb.PushScalarType("float");

    var o =
        new Operation
        {
            Left = new NumberLiteral { Value = 5f, InferredType = flt },
            Right = new NumberLiteral { Value = 6f, InferredType = flt },
            Op = OperatorToken.Plus
        };

    var s = new DeclareAssign() { TypeName = flt, VariableName = "dodo", AssignOp = AssignOpToken.Equal, Value = o };

    var o2 =
        new Operation
        {
            Left = new VariableNameLiteral("dodo"),
            Right = new NumberLiteral { Value = 6L, InferredType = flt },
            Op = OperatorToken.Plus
        };
    var s2 = new DeclareAssign() { VariableName = "dodo2", AssignOp = AssignOpToken.Equal, Value = o2 };

    var x = 0;
}


void CrossShader()
{
    WordBuffer buffer = new();

    buffer.AddOpCapability(Capability.Shader);
    buffer.AddOpExtension("SPV_GOOGLE_decorate_string");
    buffer.AddOpExtension("SPV_GOOGLE_hlsl_functionality1");
    var extInstImport = buffer.AddOpExtInstImport("GLSL.std.450");
    buffer.AddOpMemoryModel(AddressingModel.Logical, MemoryModel.GLSL450);


    // declarations

    Span<IdRef> c = stackalloc IdRef[10]; // This is for use in parameters


    var t_void = buffer.AddOpTypeVoid();

    var t_bool = buffer.AddOpTypeBool();

    var t_func = buffer.AddOpTypeFunction(t_void, Span<IdRef>.Empty);
    var t_float = buffer.AddOpTypeFloat(32);
    var t_uint = buffer.AddOpTypeInt(32, 0);
    var t_int = buffer.AddOpTypeInt(32, 1);
    var t_float4 = buffer.AddOpTypeVector(t_float, 4);
    var t_p_float4_func = buffer.AddOpTypePointer(StorageClass.Function, t_float4);
    var constant1 = buffer.AddOpConstant<LiteralFloat>(t_float, 5);
    var constant2 = buffer.AddOpConstant<LiteralFloat>(t_float, 2);
    var constant3 = buffer.AddOpConstant<LiteralInteger>(t_uint, 5);
    var compositeType = buffer.AddOpConstantComposite(
        t_float4,
        stackalloc IdRef[] { constant1, constant1, constant2, constant1 }
    );

    var t_array = buffer.AddOpTypeArray(t_float4, constant3);

    var t_struct = buffer.AddOpTypeStruct(stackalloc IdRef[] { t_uint, t_array, t_int });
    var t_struct2 = buffer.AddOpTypeStruct(stackalloc IdRef[] { t_struct, t_uint });

    var t_p_struct2 = buffer.AddOpTypePointer(StorageClass.Uniform, t_struct2);

    var v_struct2 = buffer.AddOpVariable(t_p_struct2, StorageClass.Uniform, null);

    var constant4 = buffer.AddOpConstant<LiteralInteger>(t_int, 1);

    var t_p_uint = buffer.AddOpTypePointer(StorageClass.Uniform, t_uint);
    var constant5 = buffer.AddOpConstant<LiteralInteger>(t_uint, 0);

    var t_p_output = buffer.AddOpTypePointer(StorageClass.Output, t_float4);
    var v_output = buffer.AddOpVariable(t_p_output, StorageClass.Output, null);

    var t_p_input = buffer.AddOpTypePointer(StorageClass.Input, t_float4);
    var v_input = buffer.AddOpVariable(t_p_input, StorageClass.Input, null);

    var constant6 = buffer.AddOpConstant<LiteralInteger>(t_int, 0);
    var constant7 = buffer.AddOpConstant<LiteralInteger>(t_int, 2);
    var t_p_float4_unif = buffer.AddOpTypePointer(StorageClass.Uniform, t_float4);

    var v_input_2 = buffer.AddOpVariable(t_p_input, StorageClass.Input, null);
    var t_p_func = buffer.AddOpTypePointer(StorageClass.Function, t_int);
    var constant8 = buffer.AddOpConstant<LiteralInteger>(t_int, 4);
    var v_input_3 = buffer.AddOpVariable(t_p_input, StorageClass.Input, null);




    buffer.AddOpDecorate(t_array, Decoration.ArrayStride, 16);
    buffer.AddOpMemberDecorate(t_struct, 0, Decoration.Offset, 0);
    buffer.AddOpMemberDecorate(t_struct, 1, Decoration.Offset, 16);
    buffer.AddOpMemberDecorate(t_struct, 2, Decoration.Offset, 96);
    buffer.AddOpMemberDecorate(t_struct2, 0, Decoration.Offset, 0);
    buffer.AddOpMemberDecorate(t_struct2, 1, Decoration.Offset, 112);
    buffer.AddOpDecorate(t_struct2, Decoration.Block);
    buffer.AddOpDecorate(v_struct2, Decoration.DescriptorSet, 0);
    buffer.AddOpDecorate(v_input_2, Decoration.NoPerspective);
    buffer.AddOpDecorateStringGOOGLE(v_output, Decoration.HlslSemanticGOOGLE, null, null, "COLOR");




    buffer.AddOpName(t_p_func, "main");
    buffer.AddOpName(t_struct, "S");
    buffer.AddOpMemberName(t_struct, 0, "b");
    buffer.AddOpMemberName(t_struct, 1, "v");
    buffer.AddOpMemberName(t_struct, 2, "i");


    var main = buffer.AddOpFunction(t_void, FunctionControlMask.MaskNone, t_func);
    buffer.AddOpEntryPoint(ExecutionModel.Fragment, main, "main", stackalloc IdRef[] { v_output, v_input, v_input_2, v_input_3 });
    buffer.AddOpExecutionMode(main, ExecutionMode.OriginLowerLeft);

    buffer.AddOpLabel();
    buffer.AddOpReturn();
    buffer.AddOpFunctionEnd();

    buffer.GenerateSpirv().ToHlsl();
}



void CreateMixin()
{
    var mA = 
        Mixer.Create("MixinA")
        .FinishInherit()
        .WithType("float4")
        .Build();
    var mB = 
        Mixer.Create("MixinB")
        .Inherit("MixinA")
        .FinishInherit()
        .WithType("float4x4")
        .Build();
    
    var mC = 
        Mixer.Create("MixinC")
        .Inherit("MixinA")
        .FinishInherit()
        .WithType("float4x2")
        .Build();

    var mD =
        Mixer.Create("MixinD")
        .Inherit("MixinB")
        .Inherit("MixinC")
        .FinishInherit()
        .WithType("float4x3")
        .Build();

    Console.WriteLine(mA);
    Console.WriteLine(mB);
    Console.WriteLine(mD.Disassemble());
    // var mB = new Mixin("MixinB");
    // mB.AddType<sbyte>();
    // mB.AddType<Half>();

    // MixinSourceProvider.Register(mA);
    // MixinSourceProvider.Register(mB);

    // var mC = new Mixin("MixinC");
    // mC.AddMixin("MixinA");
    // mC.AddMixin("MixinB");

    // foreach(var mix in mC)
    // {
    //     Console.WriteLine(mix.Name);
    // }

    var y = 0;
}

static void ParseWorking()
{
    var buffer = new WordBuffer();
    var mixinName = buffer.AddOpSDSLMixinName("MyMixin");

    buffer.AddOpExtInstImport("GLSL.std.450");

    var t_flt = buffer.AddOpTypeFloat(32);
    var t_vec4 = buffer.AddOpTypeVector(t_flt, 4);
    var c_3f = buffer.AddOpConstant<LiteralFloat>(t_flt,3f);
    var c_4f = buffer.AddOpConstant<LiteralFloat>(t_flt,4f);
    var c_5f = buffer.AddOpConstant<LiteralFloat>(t_flt,5f);
    var c_6f = buffer.AddOpConstant<LiteralFloat>(t_flt,6f);

    var c_vec4 = buffer.AddOpConstantComposite(t_vec4, stackalloc IdRef[]{c_3f,c_4f,c_5f,c_6f});
    
    var dis = new Disassembler();
    Console.WriteLine(dis.Disassemble(buffer));
}

// ParseWorking();

CreateMixin();



var x = 0;



// CrossShader();

// ThreeAddress();
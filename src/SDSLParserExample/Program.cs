using SDSL.Parsing;
using SDSL.Parsing.AST.Shader;
using SDSL.Parsing.AST.Shader.Analysis;
using SDSL.Parsing.AST.Shader.Symbols;
using SDSL.Parsing.Grammars.SDSL;
using SDSLParserExample;
using SoftTouch.Spirv;
using SoftTouch.Spirv.Core;
using SoftTouch.Spirv.Core.Buffers;
using SoftTouch.Spirv.Core.Parsing;
using SoftTouch.Spirv.PostProcessing;
using System.Diagnostics;
using System.Numerics;
using static Spv.Specification;

static void ThreeAddress()
{
    var symb = new SymbolTable();
    var flt = SymbolType.Scalar("float");

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
    buffer.AddOpMemoryModel(AddressingModel.Logical, MemoryModel.Vulkan);


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
        .WithType("int*", StorageClass.Function)
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

    
    //CompositionSourceProvider.CompileAndRegister("MixinC");

    var mD =
        Mixer.Create("MixinD")
        .Inherit("MixinB")
        .Inherit("MixinC")
        .FinishInherit();
    var mixin =
        mD
        .WithType("float4x3")
        .WithConstant("a", 5f)
        .WithInput("float3", "in_normal", "Normal", ExecutionModel.Vertex)
        .WithInput("float3", "in_color", "Color", ExecutionModel.Vertex)
        .WithOutput("float4", "out_position", "SV_Position", ExecutionModel.Vertex)
        .WithOutput("float3", "out_color", "Color", ExecutionModel.Vertex)
        .WithFunction("void", "DoNothing", static b => b.With("float", "myInt").With("float", "otherInt"))
            .Return()
            .FunctionEnd()
        .WithFunction("float", "ReturnOne", static b => b)
            .Return((m, f) => f.Constant(1f))
            .FunctionEnd()
        .WithEntryPoint(ExecutionModel.Vertex, "VSMain")
            .FunctionStart()
            .DeclareAssign("a", 5f)
            .Assign("out_position", (b,f) => f.Constant(new Vector4(1,2,3,0)))
            .Declare("float", "b")
            .AssignConstant("b", 6f)
            .Declare("float", "c")
            .Assign(
                "c", 
                (m, f) => 
                    f.Add(
                        "float", 
                        f.Load("a"), 
                        f.Mul(
                            "float",
                            f.Sin(f.Load("b")),
                            f.Call("ReturnOne", x => x)
                        )
                    )
            )
            .CallFunction("DoNothing", (FunctionCallerParameters p) => p.With(p.Builder.Load("a")).With(p.Builder.Add("float" ,p.Builder.Constant(8f), p.Builder.Load("a"))))
            .Return()
            .FunctionEnd()
        
        .WithCapability(Capability.Shader)
        .WithCapability(Capability.Float16)
        .WithCapability(Capability.Int8)
        .Build();

    //Console.WriteLine(mA);
    //Console.WriteLine(mB);
    //Console.WriteLine(mD.Disassemble());

    Console.WriteLine(mixin);
    var processed = PostProcessor.Process("MixinD");
    processed = PostProcessor.Process("MixinD");
    processed.Dispose();
    Stopwatch stopwatch = Stopwatch.StartNew();
    processed = PostProcessor.Process("MixinD");
    stopwatch.Stop();

    Console.WriteLine($"Process took : {stopwatch.Elapsed.TotalNanoseconds / 1000}µs");
    Console.WriteLine(Disassembler.Disassemble(processed));


    File.WriteAllBytes("./mixed.spv", processed.Bytes.ToArray());
    processed.Bytes.ToArray().ToGlsl();

    stopwatch.Restart();
    var code = processed.Bytes.ToArray().ToGlsl();
    stopwatch.Stop();
    Console.WriteLine(code);
    Console.WriteLine($"Cross compilation took : {stopwatch.Elapsed.TotalNanoseconds / 1000}µs");

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
    
    Console.WriteLine(Disassembler.Disassemble(buffer));
}

static void CheckOrderedEnumerator()
{
    var buffer = new WordBuffer();
    var t_int = buffer.AddOpTypeInt(32, 1);
    var i_var = buffer.AddOpVariable(t_int, StorageClass.Private, null);
    buffer.AddOpName(i_var, "My_var");
    buffer.AddOpTypeInt(64, 0);
    buffer.AddOpMemoryModel(AddressingModel.Logical, MemoryModel.GLSL450);
    buffer.AddOpCapability(Capability.Shader);
    buffer.AddOpCapability(Capability.Geometry);
    buffer.AddOpCapability(Capability.VectorComputeINTEL);

    foreach(var e in buffer)
    {
        Console.WriteLine(e.OpCode);
    }
}

static void ParseSDSL()
{
    var shader = File.ReadAllText(@"C:\Users\youness_kafia\Documents\dotnetProjs\SDSLParser\src\SDSLParserExample\SDSL\MixinSamples\MyShader.sdsl");
    var program = ShaderMixinParser.ParseShader(shader);
    var analyzer = new Analyzer();
    analyzer.Analyze(program);
    
    // var grammar = new SDSLGrammar();
    
    // Console.WriteLine(grammar.Match(shader).Success);
    // Console.WriteLine(grammar.Match(shader).ErrorMessage);
    var x = 0;

}


//ParseWorking();
//CheckOrderedEnumerator();
Console.WriteLine("working on " + Directory.GetCurrentDirectory());
ParseSDSL();
var t = 0;


//CrossShader();

//ThreeAddress();
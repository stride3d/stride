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
using SDSL.Mixer;
using SDSL.ThreeAddress;
using SDSL.Parsing.AST.Shader.Analysis;
using System.Text;
using SPIRVCross;
using static SPIRVCross.SPIRV;
// Directory.SetCurrentDirectory("../../../");

var shaderf = File.ReadAllText("./SDSL/shader2.sdsl");

var parser = new ShaderMixinParser();
parser.Parse(shaderf);
var x = 0;
// ShaderCompiling(shaderf);
// ThreeAddress();
// LoadShaders();
// TestModuleCreation();

// static void TestModuleCreation()
// {
//     var module = new TestModule();
//     module.Construct();
//     var code = module.Generate();
//     ToGlsl(code);
//     ToHlsl(code);
// }

// static void LoadShaders()
// {
//     // var manager = new ShaderSourceManager();
//     // manager.AddDirectory("./SDSL/MixinSamples");

//     var mixer = new SimpleMixer("SingleShader", manager);
//     var errors = mixer.SemanticChecks<VSMainMethod>();

//     var module = mixer.EmitSpirv(EntryPoints.VSMain);
//     var bytes = module.Generate();
//     File.WriteAllBytes("./shader.spv", bytes);
//     ToGlsl(bytes);
//     ToHlsl(bytes);
//     var x = 0;
// }



static void ThreeAddress()
{

    var o =
        new Operation
        {
            Left = new NumberLiteral { Value = 5f , InferredType = new ScalarType("float")},
            Right = new NumberLiteral { Value = 6f, InferredType = new ScalarType("float")},
            Op = OperatorToken.Plus
        };

    var symbols = new SymbolTable();
    var s = new DeclareAssign() { TypeName = new ScalarType("float"), VariableName = "dodo", AssignOp = AssignOpToken.Equal, Value = o };
    symbols.PushVar(s);
    var o2 =
        new Operation
        {
            Left = new VariableNameLiteral("dodo"),
            Right = new NumberLiteral { Value = 6L, InferredType = new ScalarType("float") },
            Op = OperatorToken.Plus
        };
    var s2 = new DeclareAssign() { VariableName = "dodo2", AssignOp = AssignOpToken.Equal, Value = o2 };
    symbols.PushVar(s2);

    var x = 0;
}

ThreeAddress();



// static void ShaderCompiling(string shaderf)
// {

//     var child = File.ReadAllText("./SDSL/InheritExample/Child.sdsl");
//     var parent = File.ReadAllText("./SDSL/InheritExample/Parent.sdsl");

//     // var shaderf = File.ReadAllText("../../../SDSL/shader2.sdsl");

//     var sdsl = new SDSL.Parsing.ShaderMixinParser();
//     //sdsl.Grammar.Using(sdsl.Grammar.CastExpression);
//     var s = new Stopwatch();
//     var parser = new ExpressionParser();
//     var match2 = sdsl.Parse(shaderf);
//     // sdsl.AddMacro("STRIDE_MULTISAMPLE_COUNT", 5);


//     s.Start();
//     var match = sdsl.Parse(shaderf);
//     s.Stop();

//     // sdsl.PrintParserTree();

//     Console.WriteLine(shaderf);
//     Console.WriteLine(new string('*', 64));
//     Console.WriteLine(match);
//     Console.WriteLine($"parsing time : {s.Elapsed}");

//     var grammar = ShaderParser.GetGrammar<StrideGrammar>();

//     var p = ShaderParser.GetParser<StrideGrammar>();
//     p.PreProcessAndParse(shaderf, "./SDSL/shader2.sdsl");
//     s.Start();
//     var result = p.PreProcessAndParse(shaderf, "./SDSL/shader2.sdsl");
//     s.Stop();
//     Console.WriteLine($"irony parsing time : {s.Elapsed}");
// }




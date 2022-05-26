using Eto.Parse;
using Eto.Parse.Grammars;
using Stride.Shader.Parsing;
using Stride.Shader.Parsing.Grammars.Expression;
using System.Diagnostics;
using System.Linq;



var shaderf = File.ReadAllText("../../../SDSL/shader2.sdsl");

var sdsl = new SDSLParser();
sdsl.Grammar.Using(sdsl.Grammar.CastExpression);
var s = new Stopwatch();
var parser = new ExpressionParser();
//var match2 = sdsl.Parse("#ifdef STRIDE_MULTISAMPLE_COUNT\n#endif");
//sdsl.AddMacro("STRIDE_MULTISAMPLE_COUNT", "5");


s.Start();
var match = sdsl.Parse(shaderf);
s.Stop();
Console.WriteLine(shaderf);
Console.WriteLine(new string('*', 64));
Console.WriteLine(match);
Console.WriteLine($"parsing time : {s.Elapsed}");




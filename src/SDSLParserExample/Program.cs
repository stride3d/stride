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
var match2 = sdsl.ParseDirectives("true");

s.Start();
var match = sdsl.ParseDirectives(shaderf);
s.Stop();
Console.WriteLine(new string('*', 32));
Console.WriteLine(shaderf);
Console.WriteLine(new string('*',32));
Console.WriteLine(sdsl.FinalCode);
Console.WriteLine(new string('*', 32) + "\n\n\n");
Console.WriteLine($"parsing time : {s.Elapsed}");




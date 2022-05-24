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
var match2 = parser.Parse("true");

s.Start();
var match = parser.Parse("false ? 2 ^ (float)my_value | (float)my_value | (MyStruct)++my_var.a[b].c[5].b.e[7][5] && 4 == ++b : false");
s.Stop();
Console.WriteLine($"parsing time : {s.Elapsed}");

Console.WriteLine(match);



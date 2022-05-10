using Eto.Parse;
using Eto.Parse.Grammars;
using Stride.Shader.Parsing;
using System.Diagnostics;


var shaderf = File.ReadAllText("../../../SDSL/Expressions.sdsl");

var parser = new SDSLParser();
parser.Grammar.Using(parser.Grammar.ShiftExpression.Then(";"));
var s = new Stopwatch();
var match2 = parser.Parse(shaderf);
s.Start();
var match = parser.Parse("(MyStruct)++my_var.a[0].c+6+4*5 >>2 < 5;");
s.Stop();


Console.WriteLine(match.ErrorMessage[..Math.Min(300, match.ErrorMessage.Length)]);
match.Matches.ForEach(x => Console.WriteLine(x.Name + " : " + x.Value));
Console.WriteLine($"parsing time : {s.Elapsed}");
Console.Write("");



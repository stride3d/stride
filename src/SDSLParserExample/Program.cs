using Eto.Parse;
using Eto.Parse.Grammars;
using Stride.Shader.Parsing;
using System.Diagnostics;


var shaderf = File.ReadAllText("../../../SDSL/shader2.sdsl");

var parser = new SDSLParser();
//parser.Grammar.Using(parser.Grammar.OrExpression.Then(";"));
var s = new Stopwatch();
var match2 = parser.Parse("shader MyShader<float a> : Something {}");
s.Start();
var match = parser.Parse(shaderf);
s.Stop();


Console.WriteLine(match.ErrorMessage[..Math.Min(300, match.ErrorMessage.Length)]);
match.Matches.ForEach(x => Console.WriteLine(x.Name + " : " + x.Value));
Console.WriteLine($"parsing time : {s.Elapsed}");
Console.Write("");



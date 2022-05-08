using Eto.Parse;
using Eto.Parse.Grammars;
using Stride.Shader.Parsing;
using System.Diagnostics;


var shaderf = File.ReadAllText("./SDSL/Expressions.sdsl");

var parser = new SDSLParser();
parser.Grammar.UsingPrimaryExpression();
var s = new Stopwatch();
var match2 = parser.Parse(shaderf);
s.Start();
var match = parser.Parse("a[0].b[7].a++");
s.Stop();


Console.WriteLine(match.ErrorMessage[..Math.Min(300, match.ErrorMessage.Length)]);
match.Matches.ForEach(x => Console.WriteLine(x.Name + " : " + x.Value));
Console.WriteLine($"parsing time : {s.Elapsed}");
Console.Write("");



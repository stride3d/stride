using Eto.Parse;
using Eto.Parse.Grammars;
using Stride.Shader.Parsing;
using System.Diagnostics;


var shaderf = File.ReadAllText("./SDSL/shader.sdsl");

var parser = new SDSLParser();
var sdslParser = new SDSLGrammar().UsingShader();
var s = new Stopwatch();
var match2 = parser.Parse(shaderf);
s.Start();
var match = parser.Parse(shaderf);
s.Stop();


Console.WriteLine(match.ErrorMessage);
match.Matches.ForEach(x => Console.WriteLine(x.Name + " : " + x.Value));
Console.WriteLine($"parsing time : {s.Elapsed}");
Console.Write("");



using Eto.Parse;
using Eto.Parse.Grammars;
using Stride.Shader.Parser;
using System.Diagnostics;


// var shaderf = File.ReadAllText("./shader.sdsl");

var parser = new SDSLGrammar();

var s = new Stopwatch();
s.Start();
var match = parser.Match("(((5)))");
s.Stop();

Console.WriteLine(match.ErrorMessage);
match.Matches.ForEach(x => Console.WriteLine(x.Name + " : " + x.Value));
Console.WriteLine($"parsing time : {s.Elapsed}");


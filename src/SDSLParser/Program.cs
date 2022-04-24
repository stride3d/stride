using Eto.Parse;
using Eto.Parse.Grammars;
using Stride.Shader.Parser;
using System.Diagnostics;


var shaderf = File.ReadAllText("./shader.sdsl");

// var parser = new SDSLGrammar();
var parser = StrideGrammar.New();
// var tmp = new Grammar("something", Terminals.Set("a").Not());
var s = new Stopwatch();
s.Start();
var match = parser.Match(shaderf);
// var res = SDSLPParser.Parse("My_Var");
s.Stop();

Console.WriteLine(match.ErrorMessage);
match.Matches.ForEach(x => Console.WriteLine(x.Name + " : " + x.Value));
Console.WriteLine($"parsing time : {s.Elapsed}");
Console.Write("");



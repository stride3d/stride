using Eto.Parse;
using Eto.Parse.Grammars;
using Stride.Shader.Parser;
using System.Diagnostics;


var shaderf = File.ReadAllText("./SDSL/Expressions.sdsl");

// var parser = new SDSLGrammar();
var parser = StrideGrammar.New("expr");
var tokens = StrideGrammar.HlslGrammar("expression");
var sdslParser = new SDSLGrammar().UsingStatements();
var s = new Stopwatch();
var match2 = sdslParser.Match(shaderf);
s.Start();
var match = sdslParser.Match(shaderf);
s.Stop();


Console.WriteLine(match.ErrorMessage);
match.Matches.ForEach(x => Console.WriteLine(x.Name + " : " + x.Value));
Console.WriteLine($"parsing time : {s.Elapsed}");
Console.Write("");



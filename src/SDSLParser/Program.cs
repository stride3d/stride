using Eto.Parse;
using Eto.Parse.Grammars;
using Stride.Shader.Parser;
using System.Diagnostics;


var shaderf = File.ReadAllText("./SDSL/Directive.sdsl");

// var parser = new SDSLGrammar();
var parser = StrideGrammar.New("sum");
var tokens = StrideGrammar.HlslGrammar("directives");
var s = new Stopwatch();
s.Start();
var match = tokens.Match(shaderf);
s.Stop();

Console.WriteLine(match.ErrorMessage);
match.Matches.ForEach(x => Console.WriteLine(x.Name + " : " + x.Value));
Console.WriteLine($"parsing time : {s.Elapsed}");
Console.Write("");



using Eto.Parse;
using Eto.Parse.Grammars;
using System.Diagnostics;


var shaderf = File.ReadAllText("./shader.sdsl");

var parser = new SDSLParser.SDSLGrammar();

var matched = parser.Match(shaderf);


var input = "  hello ( parsing world )  ";

// our grammar
var grammar = new EbnfGrammar(EbnfStyle.Iso14977).Build(File.ReadAllText("./grammar.ebnf"), "grammar");


var s = new Stopwatch();
s.Start();
var match = grammar.Match(shaderf);
s.Stop();
Console.WriteLine($"parsing time : {s.Elapsed}");

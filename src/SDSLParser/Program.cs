using Eto.Parse;
using Eto.Parse.Grammars;


var shaderf = File.ReadAllText("./shader.sdsl");

var parser = new SDSLParser.SDSLGrammar();

var matched = parser.Match(shaderf);

Console.WriteLine($"Hello, world!");

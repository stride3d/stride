using Eto.Parse;
using Eto.Parse.Grammars;
using Stride.Shader.Parsing;
using Stride.Shader.Parsing.Grammars.Expression;
using System.Diagnostics;
using System.Linq;



var shaderf = File.ReadAllText("../../../SDSL/shader2.sdsl");

var sdsl = new SDSLParser();
sdsl.Grammar.Using(sdsl.Grammar.CastExpression);
var s = new Stopwatch();
var parser = new ExpressionParser();
var match2 = parser.Parse("(abab) my_var");

s.Start();
var match = parser.Parse("(abab) my_var");
s.Stop();
Console.WriteLine($"parsing time : {s.Elapsed}");

Console.WriteLine(match);
//if (match.Errors.Any())
//{
//    Console.WriteLine(match.ErrorMessage[..Math.Min(10000, match.ErrorMessage.Length)]);
//    //parser.UncommentedCode.ToString().Split("\n").Select((x, i) => (x, i+1)).ToList().ForEach(x => {
//    //    Console.ForegroundColor = ConsoleColor.Blue;
//    //    Console.Write(x.Item2 + " : ");
//    //    Console.ForegroundColor = ConsoleColor.White;
//    //    Console.WriteLine(x.x);
//    //});
//}
//else
//{
//    match.Matches.ForEach(x => PrettyPrintMatches(x));
//    Console.Write("");
//}




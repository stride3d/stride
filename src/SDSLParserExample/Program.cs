using Eto.Parse;
using Eto.Parse.Grammars;
using Stride.Shader.Parsing;
using System.Diagnostics;
using System.Linq;

static void PrettyPrintMatches(Match match, int depth = 0)
{
    Console.ForegroundColor = ConsoleColor.Blue;
    Console.Write(new String(' ', depth*4) + match.Name);
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine(" : " + System.Text.RegularExpressions.Regex.Escape(match.StringValue)[..Math.Min(32,match.StringValue.Length)]);
    foreach (var m in match.Matches)
    {
        if(m.Matches.Count == 1 && m.Name.Contains("Expression"))
        {
            var tmp = m.Matches[0];
            while(tmp.Matches.Count == 1)
            {
                tmp = tmp.Matches[0];
            }
            PrettyPrintMatches(tmp, depth + 1);
        }
        else
            PrettyPrintMatches(m, depth + 1);
    }
}



var shaderf = File.ReadAllText("../../../SDSL/shader2.sdsl");

var parser = new SDSLParser();
//parser.Grammar.Using(parser.Grammar.ShaderValueDeclaration);
var s = new Stopwatch();
var match2 = parser.Parse("shader MyShader<float a> : Something {}");
s.Start();
var match = parser.Parse(shaderf);
s.Stop();

if (match.Errors.Any())
{
    Console.WriteLine(match.ErrorMessage[..Math.Min(10000, match.ErrorMessage.Length)]);
    parser.UncommentedCode.ToString().Split("\n").Select((x, i) => (x, i+1)).ToList().ForEach(x => {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.Write(x.Item2 + " : ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(x.x);
    });
}
else
{
    match.Matches.ForEach(x => PrettyPrintMatches(x));
    Console.WriteLine($"parsing time : {s.Elapsed}");
    Console.Write("");
}




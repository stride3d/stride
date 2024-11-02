using Stride.Shaders.Experiments;
using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders.Parsing.SDSL.AST;


// Examples.SpvOpt();
// Examples.TranslateHLSL();
var matched = Grammar.Match<StatementParsers, Statement>("if(depth < 0 || depth > 1)\n    return 1;");
foreach(var e in matched.Errors)
    Console.WriteLine(e);
Console.WriteLine(matched.AST);

// Examples.ParseSDSL();

// Examples.TryAllFiles();
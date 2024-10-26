using Stride.Shaders.Experiments;
using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders.Parsing.SDSL.AST;


// Examples.SpvOpt();
// Examples.TranslateHLSL();
Grammar.Match<ExpressionParser, Expression>("float(num) / 4294967295.0");
Examples.ParseSDSL();

Examples.TryAllFiles();
using Stride.Shaders.Experiments;
using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders.Parsing.SDSL.AST;


// Examples.SpvOpt();
// Examples.TranslateHLSL();
Grammar.Match<StatementParsers, Statement>("{\nsamplePosition += BackfaceOffsets[lightIndex];}");
Examples.ParseSDSL();

Examples.TryAllFiles();
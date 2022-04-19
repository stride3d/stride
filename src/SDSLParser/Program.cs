using Eto.Parse;
using Eto.Parse.Grammars;

var ws = Terminals.WhiteSpace.Repeat(0);

// parse a value with or without brackets
var valueParser = Terminals.Set('(')
	.Then(Terminals.AnyChar.Repeat().Until(ws.Then(')')).Named("value"))
	.Then(Terminals.Set(')'))
	.SeparatedBy(ws)
	.Or(Terminals.WhiteSpace.Inverse().Repeat().Named("value"));

// our grammar
var grammar = new Grammar(
	ws
	.Then(valueParser.Named("first"))
	.Then(valueParser.Named("second"))
	.Then(Terminals.End)
	.SeparatedBy(ws)
);
var grammar2 = new Grammar(
	ws
	.Then(Terminals.Set("shader"))
	.Then(valueParser.Named("second"))
	.Then(Terminals.End)
	.SeparatedBy(ws)
);

var input = "  jojo ( vs dio )  ";
var matched = grammar.Match(input);
var parsed = grammar.Parse(new ParseArgs(null));
var value = matched["second"]["value"].Value;
Console.WriteLine($"Hello, {value}!");

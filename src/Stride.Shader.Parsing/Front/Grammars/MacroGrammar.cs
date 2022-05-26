using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;


namespace Stride.Shader.Parsing.Grammars.Macros;

public class MacroGrammar : Grammar
{

    public MacroGrammar(params string[] variableNames) : base("variables-sdsl")
    {
        var altVars = new AlternativeParser(variableNames.Select(Literal))
        {
            Name = "MacroVariable"
        };
        var grammar = new AlternativeParser(
            AnyChar.Repeat(0).Until(altVars).Named("ActualCode") & altVars
            | AnyChar.Repeat(0).Until(End).Named("ActualCode")
        );
        Inner = grammar.Repeat(0);
    }
}

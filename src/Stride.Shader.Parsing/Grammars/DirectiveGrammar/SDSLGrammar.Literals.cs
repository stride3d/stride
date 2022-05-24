using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

using EtoParser = Eto.Parse.Parser;

namespace Stride.Shader.Parsing.Grammars.Directive;
public partial class DirectiveGrammar : Grammar
{
	AlternativeParser IntegerSuffix = new();
	AlternativeParser FloatSuffix = new();
    
	public StringParser StringLiteral = new();
	public SequenceParser Identifier = new();

    public NumberParser IntegerLiteral = new();
	public NumberParser FloatLiteral = new();
	public HexDigitTerminal HexDigits = new();
    public SequenceParser HexaDecimalLiteral = new();

	public BooleanTerminal BooleanTerm = new();
    
	public AlternativeParser Literals = new();

	public void CreateLiterals()
	{
		Identifier.Add(
			Letter.Or("_").Then(LetterOrDigit.Or("_").Repeat(0)).WithName("Identifier")
		);

		IntegerSuffix.Add(
			"u",
			"l",
			"U",
			"L"
		);
		
		FloatSuffix.Add(
			"f",
			"d",
			"F",
			"D"
		);
		
		
		
		StringLiteral = new StringParser().WithName("StringLiteral");
		IntegerLiteral = new NumberParser() { AllowSign = true, AllowDecimal = false, AllowExponent = false, ValueType = typeof(long), Name = "float_value"}.WithName("IntegerValue");
		FloatLiteral = new NumberParser() { AllowSign = true, AllowDecimal = true, AllowExponent = true, ValueType = typeof(double), Name = "int_value"}.WithName("FloatValue");
		HexDigits = new();
		HexaDecimalLiteral = Literal("0x").Or(Literal("0X")).Then(HexDigit.Repeat(1)).WithName("HexaLiteral");

		BooleanTerm = new BooleanTerminal{CaseSensitive = true, TrueValues = new string[]{"true"},FalseValues = new string[]{"false"}, Name = "Boolean"};

		Literals.Add(
			IntegerLiteral.NotFollowedBy(Dot | FloatSuffix | Set("xX")).Named("IntegerLiteral"),
			FloatLiteral.NotFollowedBy(Set("xX")).Then(FloatSuffix.Optional()).Named("FloatLiteral"),
			HexaDecimalLiteral,
			StringLiteral,
			BooleanTerm
		);		
	}
}
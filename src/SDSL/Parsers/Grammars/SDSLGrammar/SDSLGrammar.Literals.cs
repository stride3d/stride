using System.Globalization;
using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;
using static SDSL.Parsing.Grammars.CommonParsers;

namespace SDSL.Parsing.Grammars.SDSL;
public partial class SDSLGrammar : Grammar
{    
	public StringParser StringLiteral = new();
    public AlternativeParser UserDefinedId = new();

    public NumberParser IntegerLiteral = new();
	public NumberParser FloatLiteral = new();
	public HexDigitTerminal HexDigits = new();
    public SequenceParser HexaDecimalLiteral = new();

	public BooleanTerminal BooleanTerm = new() { Name = "BooleanTerm"};
    
	public AlternativeParser Literals = new();

	public SDSLGrammar UsingLiterals()
	{
		Inner = Literals;
		return this;
	}
	public void CreateLiterals()
	{
		
		
		StringLiteral = new StringParser().WithName("StringLiteral");
		IntegerLiteral = new NumberParser() { AllowSign = true, AllowDecimal = false, AllowExponent = false, ValueType = typeof(long), Name = "IntegerValue" };
		FloatLiteral = new NumberParser() { AllowSign = true, AllowDecimal = true, AllowExponent = true, ValueType = typeof(double), Culture = new CultureInfo("en-US") , Name = "FloatValue" };
		
		HexDigits = new();
		HexaDecimalLiteral = Literal("0x").Or(Literal("0X")).Then(HexDigit.Repeat(1)).WithName("HexaLiteral");

		BooleanTerm = new BooleanTerminal{CaseSensitive = true, TrueValues = ["true"],FalseValues = ["false"], Name = "Boolean"};

		Literals.Add(
            IntegerLiteral.NotFollowedBy(Dot | IntegerSuffix | FloatSuffix | Set("xX")).Named("IntegerLiteral"),
            IntegerLiteral.NotFollowedBy(Dot | FloatSuffix | Set("xX")).Then(IntegerSuffix).Named("IntegerLiteral"),
            FloatLiteral.NotFollowedBy(Set("xX")).Then(FloatSuffix).Named("FloatLiteral"),
            FloatLiteral.NotFollowedBy(Set("xX")).Named("FloatLiteral"),
            HexaDecimalLiteral,
			StringLiteral,
			BooleanTerm
		);		
	}
}
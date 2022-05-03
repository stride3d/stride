using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

using EtoParser = Eto.Parse.Parser;

namespace Stride.Shader.Parser;
public partial class SDSLGrammar : Grammar
{
	AlternativeParser IntegerSuffix;
	AlternativeParser FloatSuffix;
	
	public SequenceParser SingleLineComment = new(); 
	public SequenceParser BlockComment = new(); 
    
	public StringParser StringLiteral = new();
	public EtoParser Identifier;
    public NumberParser IntegerLiteral = new();
	public NumberParser FloatLiteral = new();
	public HexDigitTerminal HexDigits = new();
    public SequenceParser HexaDecimalLiteral = new();

	public BooleanTerminal BooleanTerm = new();
    
	public AlternativeParser Literals = new();

	public SDSLGrammar UsingLiterals()
	{
		Inner = Literals;
		return this;
	}
	public void CreateLiterals()
	{
		Identifier = Letter.Or("_").Then(LetterOrDigit.Or("_").Repeat(0)).WithName("Identifier");
		IntegerSuffix = 
			Literal("u")
			| Literal("l")
			| Literal("U")
			| Literal("L");
		
		FloatSuffix = 
			Literal("f")
			| Literal("d")
			| Literal("F")
			| Literal("D");
		
		SingleLineComment = Literal("//").Then(AnyChar.Repeat(0).Until(Eol)).WithName("LineComment"); 
		BlockComment = Literal("/*").Then(AnyChar.Repeat(0).Until("*/",false,true)).WithName("BlockComment"); 
		
		StringLiteral = new StringParser().WithName("StringLiteral");
		IntegerLiteral = new NumberParser() { AllowSign = true, AllowDecimal = false, AllowExponent = false, ValueType = typeof(long), Name = "float_value"}.WithName("IntegerValue");
		FloatLiteral = new NumberParser() { AllowSign = true, AllowDecimal = true, AllowExponent = true, ValueType = typeof(double), Name = "int_value"}.WithName("FloatValue");
		HexDigits = new();
		HexaDecimalLiteral = Literal("0x").Or(Literal("0X")).Then(HexDigit.Repeat(1)).WithName("HexaLiteral");
		
		var ints = 
			HexaDecimalLiteral
			| IntegerLiteral.Then(IntegerSuffix.Optional().Named("suffix")).Named("IntegerLiteral");
		var floats = 
			FloatLiteral.Then(FloatSuffix.Optional().Named("suffix")).Named("FloatLiteral");
			// | IntegerLiteral.Then(IntegerSuffix);

		BooleanTerm = new BooleanTerminal{CaseSensitive = true, TrueValues = new string[]{"true"},FalseValues = new string[]{"false"}, Name = "Boolean"};

		Literals.Add(
			floats - ints
			| ints
			| StringLiteral
		);		
	}
}
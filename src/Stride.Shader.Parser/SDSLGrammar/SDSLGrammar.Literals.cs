using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace Stride.Shader.Parser;
public partial class SDSLGrammar : Grammar
{
	AlternativeParser IntegerSuffix;
	AlternativeParser FloatSuffix;
	
	public SequenceParser SingleLineComment = new(); 
	public SequenceParser BlockComment = new(); 
    
	public StringParser StringLiteral = new();
	public SequenceParser Identifier = new();
    public NumberParser IntegerLiteral = new();
	public NumberParser FloatLiteral = new();
	public HexDigitTerminal HexDigits = new();
    public SequenceParser HexaDecimalLiteral = new();
    
	public AlternativeParser Literals = new();

	public void UsingLiterals()
	{
		Inner = Literals;
	}
	public void CreateLiterals()
	{
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
		Identifier = Letter.Or("_").Then(LetterOrDigit.Or("_").Repeat(0)).WithName("Identifier");
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

		Literals.Add(
			floats - ints
			| ints
			| StringLiteral
		);
			// .NotFollowedBy(IntegerSuffix) - FloatLiteral
			// | IntegerLiteral.Then(IntegerSuffix) - FloatLiteral 
			// // | FloatLiteral.NotFollowedBy(FloatSuffix)
			// // | FloatLiteral.Then(FloatSuffix)
			// // | HexaDecimalLiteral
			// | StringLiteral;
		
	}
}
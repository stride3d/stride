using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace Stride.Shader.Parser;
public partial class SDSLGrammar : Grammar
{
	AlternativeParser IntegerSuffix = new();
	AlternativeParser FloatSuffix = new();
	
	public SequenceParser SingleLineComment = new(); 
	public SequenceParser BlockComment = new(); 
    
	public StringParser StringLiteral = new();
	public SequenceParser Identifier = new();
    public SequenceParser IntegerLiteral = new();
	public SequenceParser FloatLiteral = new();
	public HexDigitTerminal HexDigits = new();
    public SequenceParser HexaDecimalLiteral = new();
    
	public AlternativeParser Literals = new();

	public void UsingLiterals()
	{
		Inner = Literals;
	}
	public void CreateLiterals()
	{
		IntegerSuffix = (Set("u") | "l" | "U" | "L").WithName("suffix");
		FloatSuffix = (Set("f") | "l" | "F" | "L").WithName("suffix");
		
		SingleLineComment = Set("//").Then(AnyChar.Repeat(0).Until(Eol)).WithName("LineComment"); 
		BlockComment = Set("/*").Then(AnyChar.Repeat(0).Until("*/",false,true)).WithName("BlockComment"); 
		
		StringLiteral = new StringParser().WithName("StringLiteral");
		Identifier = Letter.Or("_").Then(LetterOrDigit.Or("_").Repeat(0)).WithName("Identifier");
		IntegerLiteral = new NumberParser() { AllowSign = true, AllowDecimal = false, AllowExponent = false, ValueType = typeof(long), Name = "value"}.NotFollowedBy(FloatSuffix).Then(IntegerSuffix.Optional()).WithName("IntegerLiteral");
		FloatLiteral = new NumberParser() { AllowSign = true, AllowDecimal = true, AllowExponent = true, ValueType = typeof(double), Name = "value"}.NotFollowedBy(IntegerSuffix).Then(FloatSuffix.Optional()).WithName("FloatLiteral");
		HexDigits = new();
		HexaDecimalLiteral = Set("0x").Or("0X").Then(HexDigit.Repeat(1)).WithName("HexaLiteral");
		
		Literals =
			IntegerLiteral
			| FloatLiteral
			| HexaDecimalLiteral
			| StringLiteral;
		
	}
}
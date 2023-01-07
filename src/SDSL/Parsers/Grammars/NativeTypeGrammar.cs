using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;


namespace SDSL.Parsing.Grammars.NativeType;

public class NativeTypeGrammar : Grammar
{
    static NativeTypeGrammar global = new();

    public static Match ParseNativeType(string s) => global.Match(s);

    public NativeTypeGrammar() : base("native-type-sdsl")
    {
        var numbers = new NumberParser(){AllowDecimal = false, AllowSign = false, AllowExponent = false, ValueType = typeof(int)};
        Inner = new AlternativeParser(
            Literal("bool").Then(numbers.Named("RowCount")).Then("x").Then(numbers.Named("ColCount")).Named("BoolMatrix"),

            Literal("ulong").Then(numbers.Named("RowCount")).Then("x").Then(numbers.Named("ColCount")).Named("ULongMatrix"),
            Literal("long").Then(numbers.Named("RowCount")).Then("x").Then(numbers.Named("ColCount")).Named("LongMatrix"),
            Literal("double").Then(numbers.Named("RowCount")).Then("x").Then(numbers.Named("ColCount")).Named("DoubleMatrix"),
            
            Literal("uint").Then(numbers.Named("RowCount")).Then("x").Then(numbers.Named("ColCount")).Named("UIntMatrix"),
            Literal("int").Then(numbers.Named("RowCount")).Then("x").Then(numbers.Named("ColCount")).Named("IntMatrix"),
            Literal("float").Then(numbers.Named("RowCount")).Then("x").Then(numbers.Named("ColCount")).Named("FloatMatrix"),

            Literal("ushort").Then(numbers.Named("RowCount")).Then("x").Then(numbers.Named("ColCount")).Named("UShortMatrix"),
            Literal("short").Then(numbers.Named("RowCount")).Then("x").Then(numbers.Named("ColCount")).Named("ShortMatrix"),
            Literal("half").Then(numbers.Named("RowCount")).Then("x").Then(numbers.Named("ColCount")).Named("HalfMatrix"),

            Literal("byte").Then(numbers.Named("RowCount")).Then("x").Then(numbers.Named("ColCount")).Named("ByteMatrix"),
            Literal("sbyte").Then(numbers.Named("RowCount")).Then("x").Then(numbers.Named("ColCount")).Named("SByteMatrix"),

            Literal("bool").Then(numbers.Named("RowCount")).Named("BoolVector"),

            Literal("ulong").Then(numbers.Named("RowCount")).Named("ULongVector"),
            Literal("long").Then(numbers.Named("RowCount")).Named("LongVector"),
            Literal("double").Then(numbers.Named("RowCount")).Named("DoubleVector"),
            
            Literal("uint").Then(numbers.Named("RowCount")).Named("UIntVector"),
            Literal("int").Then(numbers.Named("RowCount")).Named("IntVector"),
            Literal("float").Then(numbers.Named("RowCount")).Named("FloatVector"),

            Literal("ushort").Then(numbers.Named("RowCount")).Named("UShortVector"),
            Literal("short").Then(numbers.Named("RowCount")).Named("ShortVector"),
            Literal("half").Then(numbers.Named("RowCount")).Named("HalfVector"),

            Literal("byte").Then(numbers.Named("RowCount")).Named("ByteVector"),
            Literal("sbyte").Then(numbers.Named("RowCount")).Named("SByteVector"),
            

            Literal("bool").Named("Bool"),

            Literal("ulong").Named("ULong"),
            Literal("long").Named("Long"),
            Literal("double").Named("Double"),
            
            Literal("uint").Named("UInt"),
            Literal("int").Named("Int"),
            Literal("float").Named("Float"),

            Literal("ushort").Named("UShort"),
            Literal("short").Named("Short"),
            Literal("half").Named("Half"),

            Literal("byte").Named("Byte"),
            Literal("sbyte").Named("SByte"),
            Terminals.Letter.Or("_").Then(Terminals.LetterOrDigit.Or("_").Repeat(0))        
        ).WithName("TypeParser");
    }
}

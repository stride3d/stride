using Eto.Parse;
using Eto.Parse.Parsers;

namespace SDSL.Parsing.Grammars;

public static class CommonParsers
{

    static CommonParsers()
    {
        TextureBase =
            (
                Terminals.Literal("Texture2DMS")
                | Terminals.Literal("TextureCube")
                | Terminals.Literal("Texture") & Terminals.Set("123") & "D"
            ) & ~Terminals.Literal("Array");
        TextureTypes =
            (TextureBase & "<" & (BuiltinNumericTypes | Identifier) & ">").SeparatedBy(Spaces)
            | TextureBase;
        BufferTypes =
            (Buffer & "<" & (BuiltinNumericTypes | Identifier) & ">").SeparatedBy(Spaces)
            | Buffer;

    }
    public static SequenceParser Identifier { get; } = (Terminals.Letter | "_") & (Terminals.Letter | Terminals.Digit | "_") * 0;
    public static SequenceParser TextureBase { get; }
    public static AlternativeParser TextureTypes { get; }
    public static AlternativeParser BufferTypes { get; }
    public static Parser Space => Terminals.WhiteSpace;
    public static RepeatParser Spaces { get; } = Terminals.WhiteSpace * 0;
    public static RepeatParser Spaces1 { get; } = Terminals.WhiteSpace * 1;
    public static LiteralTerminal AppendStructuredBuffer { get; } = new();
    public static AlternativeParser ComponentNumber { get; } = new();

    public static SequenceParser BuiltinNumericTypes { get; } =
        (Terminals.Literal("void") | "void" | "byte" | "sbyte" | "short" | "ushort" | "half" | "int" | "uint" | "float" | "long" | "ulong" | "double")
        & (
            (Terminals.Set("234") & "x" & Terminals.Set("234"))
            | Terminals.Set("234")
        );

    public static CharTerminal IntegerSuffix { get; } = Terminals.Set("ulUL").WithName("IntegerSuffix");
    public static CharTerminal FloatSuffix { get; } = Terminals.Set("fdFD").WithName("FloatSuffix");
    public static LiteralTerminal Buffer { get; } = Terminals.Literal("buffer").WithName("Buffer");
    public static LiteralTerminal ByteAddressBuffer { get; } = Terminals.Literal("ByteAddressBuffer").WithName("ByteAddressBuffer");
    public static LiteralTerminal Break { get; } = Terminals.Literal("break").WithName("Break");
    public static LiteralTerminal Case { get; } = Terminals.Literal("case").WithName("Case");
    public static LiteralTerminal CBuffer { get; } = Terminals.Literal("cBuffer").WithName("CBuffer");
    public static LiteralTerminal Centroid { get; } = Terminals.Literal("centroid").WithName("Centroid");
    public static LiteralTerminal Class { get; } = Terminals.Literal("class").WithName("Class");
    public static LiteralTerminal ColumnMajor { get; } = Terminals.Literal("ColumnMajor").WithName("ColumnMajor");
    public static LiteralTerminal Const { get; } = Terminals.Literal("const").WithName("Const");
    public static LiteralTerminal ConsumeStructuredBuffer { get; } = Terminals.Literal("ConsumeStructuredBuffer").WithName("ConsumeStructuredBuffer");
    public static LiteralTerminal Continue { get; } = Terminals.Literal("continue").WithName("Continue");
    public static LiteralTerminal Default { get; } = Terminals.Literal("default").WithName("Default");
    public static LiteralTerminal Discard { get; } = Terminals.Literal("discard").WithName("Discard");
    public static LiteralTerminal Do { get; } = Terminals.Literal("do").WithName("Do");
    public static LiteralTerminal Else { get; } = Terminals.Literal("else").WithName("Else");
    public static LiteralTerminal Extern { get; } = Terminals.Literal("extern").WithName("Extern");
    public static LiteralTerminal For { get; } = Terminals.Literal("for").WithName("For");
    public static LiteralTerminal Groupshared { get; } = Terminals.Literal("groupshared").WithName("Groupshared");
    public static LiteralTerminal If { get; } = Terminals.Literal("if").WithName("If");
    public static LiteralTerminal In { get; } = Terminals.Literal("in").WithName("In");
    public static LiteralTerminal Inout { get; } = Terminals.Literal("inout").WithName("Inout");
    public static LiteralTerminal InputPatch { get; } = Terminals.Literal("inputpatch").WithName("InputPatch");
    public static LiteralTerminal Interface { get; } = Terminals.Literal("interface").WithName("Interface");

    // public LiteralTerminal Line_ { get; set; }

    public static LiteralTerminal LineAdj { get; } = Terminals.Literal("lineAdj").WithName("LineAdj");
    public static LiteralTerminal Linear { get; } = Terminals.Literal("linear").WithName("Linear");
    public static LiteralTerminal LineStream { get; } = Terminals.Literal("LineStream").WithName("LineStream");
    public static LiteralTerminal Matrix { get; } = Terminals.Literal("matrix").WithName("Matrix");
    public static LiteralTerminal Nointerpolation { get; } = Terminals.Literal("nointerpolation").WithName("Nointerpolation");
    public static LiteralTerminal Noperspective { get; } = Terminals.Literal("noperspective").WithName("Noperspective");
    public static LiteralTerminal Out { get; } = Terminals.Literal("out").WithName("Out");
    public static LiteralTerminal OutputPatch { get; } = Terminals.Literal("OutputPatch").WithName("OutputPatch");
    public static LiteralTerminal Packoffset { get; } = Terminals.Literal("packoffset").WithName("Packoffset");
    public static LiteralTerminal Point { get; } = Terminals.Literal("point").WithName("Point");
    public static LiteralTerminal PointStream { get; } = Terminals.Literal("PointStream").WithName("PointStream");
    public static LiteralTerminal Precise { get; } = Terminals.Literal("precise").WithName("Precise");
    public static LiteralTerminal Register { get; } = Terminals.Literal("register").WithName("Register");
    public static LiteralTerminal Return { get; } = Terminals.Literal("return").WithName("Return");
    public static LiteralTerminal RowMajor { get; } = Terminals.Literal("rowMajor").WithName("RowMajor");
    public static LiteralTerminal RWBuffer { get; } = Terminals.Literal("RWBuffer").WithName("RWBuffer");
    public static LiteralTerminal RWByteAddressBuffer { get; } = Terminals.Literal("RWByteAddressBuffer").WithName("RWByteAddressBuffer");
    public static LiteralTerminal RWStructuredBuffer { get; } = Terminals.Literal("RWStructuredBuffer").WithName("RWStructuredBuffer");
    public static LiteralTerminal Sample { get; } = Terminals.Literal("sample").WithName("Sample");
    public static LiteralTerminal Sampler { get; } = Terminals.Literal("sampler").WithName("Sampler");
    public static LiteralTerminal SamplerComparisonState { get; } = Terminals.Literal("SamplerComparisonState").WithName("SamplerComparisonState");
    public static LiteralTerminal SamplerState { get; } = Terminals.Literal("SamplerState").WithName("SamplerState");
    public static LiteralTerminal Shared { get; } = Terminals.Literal("shared").WithName("Shared");
    public static LiteralTerminal StaticConst { get; } = Terminals.Literal("staticConst").WithName("StaticConst");
    public static LiteralTerminal Static { get; } = Terminals.Literal("static").WithName("Static");
    public static LiteralTerminal Struct { get; } = Terminals.Literal("struct").WithName("Struct");
    public static LiteralTerminal StructuredBuffer { get; } = Terminals.Literal("StructuredBuffer").WithName("StructuredBuffer");
    public static LiteralTerminal Switch { get; } = Terminals.Literal("switch").WithName("Switch");
    public static LiteralTerminal Triangle { get; } = Terminals.Literal("triangle").WithName("Triangle");
    public static LiteralTerminal TriangleAdj { get; } = Terminals.Literal("triangleadj").WithName("TriangleAdj");
    public static LiteralTerminal TriangleStream { get; } = Terminals.Literal("TriangleStream").WithName("TriangleStream");
    public static LiteralTerminal Uniform { get; } = Terminals.Literal("uniform").WithName("Uniform");
    public static LiteralTerminal Vector { get; } = Terminals.Literal("vector").WithName("Vector");
    public static LiteralTerminal Volatile { get; } = Terminals.Literal("volatile").WithName("Volatile");
    public static LiteralTerminal Void { get; } = Terminals.Literal("void").WithName("Void");
    public static LiteralTerminal While { get; } = Terminals.Literal("while").WithName("While");
    public static LiteralTerminal LeftParen { get; } = Terminals.Literal("(").WithName("LeftParen");
    public static LiteralTerminal RightParen { get; } = Terminals.Literal(")").WithName("RightParen");
    public static LiteralTerminal LeftBracket { get; } = Terminals.Literal("[").WithName("LeftBracket");
    public static LiteralTerminal RightBracket { get; } = Terminals.Literal("]").WithName("RightBracket");
    public static LiteralTerminal LeftBrace { get; } = Terminals.Literal("{").WithName("LeftBrace");
    public static LiteralTerminal RightBrace { get; } = Terminals.Literal("}").WithName("RightBrace");
    public static LiteralTerminal LeftShift { get; } = Terminals.Literal("<<").WithName("LeftShift");
    public static LiteralTerminal RightShift { get; } = Terminals.Literal(">>").WithName("RightShift");
    public static LiteralTerminal Plus { get; } = Terminals.Literal("+").WithName("Plus");
    public static LiteralTerminal PlusPlus { get; } = Terminals.Literal("++").WithName("PlusPlus");
    public static LiteralTerminal Minus { get; } = Terminals.Literal("-").WithName("Minus");
    public static LiteralTerminal MinusMinus { get; } = Terminals.Literal("--").WithName("MinusMinus");
    public static LiteralTerminal Star { get; } = Terminals.Literal("*").WithName("Star");
    public static LiteralTerminal Div { get; } = Terminals.Literal("/").WithName("Div");
    public static LiteralTerminal Mod { get; } = Terminals.Literal("%").WithName("Mod");
    public static LiteralTerminal And { get; } = Terminals.Literal("&").WithName("And");
    public static LiteralTerminal Or { get; } = Terminals.Literal("|").WithName("Or");
    public static LiteralTerminal AndAnd { get; } = Terminals.Literal("&&").WithName("AndAnd");
    public static LiteralTerminal OrOr { get; } = Terminals.Literal("||").WithName("OrOr");
    public static LiteralTerminal Caret { get; } = Terminals.Literal("^").WithName("Caret");
    public static LiteralTerminal Not { get; } = Terminals.Literal("!").WithName("Not");
    public static LiteralTerminal Tilde { get; } = Terminals.Literal("~").WithName("Tilde");
    public static LiteralTerminal Equal { get; } = Terminals.Literal("==").WithName("Equal");
    public static LiteralTerminal NotEqual { get; } = Terminals.Literal("!=").WithName("NotEqual");
    public static LiteralTerminal Less { get; } = Terminals.Literal("<").WithName("Less");
    public static LiteralTerminal LessEqual { get; } = Terminals.Literal("<=").WithName("LessEqual");
    public static LiteralTerminal Greater { get; } = Terminals.Literal(">").WithName("Greater");
    public static LiteralTerminal GreaterEqual { get; } = Terminals.Literal(">=").WithName("GreaterEqual");
    public static LiteralTerminal Question { get; } = Terminals.Literal("?").WithName("Question");
    public static LiteralTerminal Colon { get; } = Terminals.Literal(":").WithName("Colon");
    public static LiteralTerminal ColonColon { get; } = Terminals.Literal("::").WithName("ColonColon");
    public static LiteralTerminal Semi { get; } = Terminals.Literal(";").WithName("Semi");
    public static LiteralTerminal Comma { get; } = Terminals.Literal(",").WithName("Comma");
    public static LiteralTerminal Assign { get; } = Terminals.Literal("=").WithName("Assign");
    public static LiteralTerminal StarAssign { get; } = Terminals.Literal("*=").WithName("StarAssign");
    public static LiteralTerminal DivAssign { get; } = Terminals.Literal("/=").WithName("DivAssign");
    public static LiteralTerminal ModAssign { get; } = Terminals.Literal("%=").WithName("ModAssign");
    public static LiteralTerminal PlusAssign { get; } = Terminals.Literal("+=").WithName("PlusAssign");
    public static LiteralTerminal MinusAssign { get; } = Terminals.Literal("-=").WithName("MinusAssign");
    public static LiteralTerminal LeftShiftAssign { get; } = Terminals.Literal("<<=").WithName("LeftShiftAssign");
    public static LiteralTerminal RightShiftAssign { get; } = Terminals.Literal(">>=").WithName("RightShiftAssign");
    public static LiteralTerminal AndAssign { get; } = Terminals.Literal("&=").WithName("AndAssign");
    public static LiteralTerminal XorAssign { get; } = Terminals.Literal("^=").WithName("XorAssign");
    public static LiteralTerminal OrAssign { get; } = Terminals.Literal("|=").WithName("OrAssign");

    public static LiteralTerminal Dot { get; } = Terminals.Literal(".").WithName("Dot");
    public static LiteralTerminal True { get; } = Terminals.Literal("true").WithName("True");
    public static LiteralTerminal False { get; } = Terminals.Literal("false").WithName("False");
    public static AlternativeParser PreprocessorDirectiveName { get; } =
        (Terminals.Literal("define")
        | "elif"
        | "else"
        | "endif"
        | "error"
        | "if"
        | "ifdef"
        | "ifndef"
        | "include"
        | "line"
        | "pragma"
        | "undef").WithName("PreprocessorDirectiveName");

    public static LiteralTerminal Compose { get; } = Terminals.Literal("compose").WithName("Compose");
    public static LiteralTerminal Stream { get; } = Terminals.Literal("stream").WithName("Stream");
    public static LiteralTerminal Stage { get; } = Terminals.Literal("stage").WithName("Stage");
}
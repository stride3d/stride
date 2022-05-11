using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;
namespace Stride.Shader.Parsing;
public partial class SDSLGrammar : Grammar
{
    private CharTerminal WS;
    private AlternativeParser Space =  new();
    private RepeatParser Spaces = new();
    private SequenceParser SpacesWithLineBreak =  new();
    private LiteralTerminal AppendStructuredBuffer = new();
    private AlternativeParser ComponentNumber =  new();
    
    private LiteralTerminal Bool = new();
    private SequenceParser BoolVec =  new();
    private SequenceParser BoolMat =  new();
    private AlternativeParser Uint =  new();
    private SequenceParser UintVec =  new();
    private SequenceParser UintMat =  new();
    private LiteralTerminal Int = new();
    private SequenceParser IntVec =  new();
    private SequenceParser IntMat =  new();
    
    private LiteralTerminal Half = new();
    private SequenceParser HalfVec =  new();
    private SequenceParser HalfMat =  new();
    private LiteralTerminal Float = new();
    private SequenceParser FloatVec =  new();
    private SequenceParser FloatMat =  new();
    private LiteralTerminal Double = new();
    private SequenceParser DoubleVec =  new();
    private SequenceParser DoubleMat =  new();
    private LiteralTerminal Buffer = new();
    private LiteralTerminal ByteAddressBuffer = new();
    private LiteralTerminal Break = new();
    private LiteralTerminal Case = new();
    private LiteralTerminal CBuffer = new();
    private LiteralTerminal Centroid = new();
    private LiteralTerminal Class = new();
    private LiteralTerminal ColumnMajor = new();
    private LiteralTerminal Const = new();
    private LiteralTerminal ConsumeStructuredBuffer = new();
    private LiteralTerminal Continue = new();
    private LiteralTerminal Default = new();
    private LiteralTerminal Discard = new();
    private LiteralTerminal Do = new();
    private LiteralTerminal Else = new();
    private LiteralTerminal Extern = new();
    private LiteralTerminal For = new();
    private LiteralTerminal Groupshared = new();
    private LiteralTerminal If = new();
    private LiteralTerminal In = new();
    private AlternativeParser Inout =  new();
    private LiteralTerminal InputPatch = new();
    private LiteralTerminal Interface = new();

    public LiteralTerminal Line_ { get; private set; }

    private LiteralTerminal LineAdj = new();
    private LiteralTerminal Linear = new();
    private LiteralTerminal LineStream = new();
    private LiteralTerminal Long = new();
    private LiteralTerminal Matrix = new();
    private LiteralTerminal Nointerpolation = new();
    private LiteralTerminal Noperspective = new();
    private LiteralTerminal Out = new();
    private LiteralTerminal OutputPatch = new();
    private LiteralTerminal Packoffset = new();
    private LiteralTerminal Point = new();
    private LiteralTerminal PointStream = new();
    private LiteralTerminal Precise = new();
    private LiteralTerminal Register = new();
    private LiteralTerminal Return = new();
    private LiteralTerminal RowMajor = new();
    private LiteralTerminal RWBuffer = new();
    private LiteralTerminal RWByteAddressBuffer = new();
    private LiteralTerminal RWStructuredBuffer = new();
    private LiteralTerminal Sample = new();
    private LiteralTerminal Sampler = new();
    private LiteralTerminal SamplerComparisonState = new();
    private LiteralTerminal SamplerState = new();
    private LiteralTerminal Shared = new();
    private LiteralTerminal Static = new();
    private LiteralTerminal Struct = new();
    private LiteralTerminal StructuredBuffer = new();
    private LiteralTerminal Switch = new();
    private AlternativeParser TextureTypes = new();
    private LiteralTerminal Triangle = new();
    private LiteralTerminal TriangleAdj = new();
    private LiteralTerminal TriangleStream = new();
    private LiteralTerminal Uniform = new();
    private LiteralTerminal Vector = new();
    private LiteralTerminal Volatile = new();
    private LiteralTerminal Void = new();
    private LiteralTerminal While = new();
    private LiteralTerminal LeftParen = new();
    private LiteralTerminal RightParen = new();
    private LiteralTerminal LeftBracket = new();
    private LiteralTerminal RightBracket = new();
    private LiteralTerminal LeftBrace = new();
    private LiteralTerminal RightBrace = new();

    private LiteralTerminal LeftShift = new();
    private LiteralTerminal RightShift = new();
    private LiteralTerminal Plus = new();
    private LiteralTerminal PlusPlus = new();
    private LiteralTerminal Minus = new();
    private LiteralTerminal MinusMinus = new();
    private LiteralTerminal Star = new();
    private LiteralTerminal Div = new();
    private LiteralTerminal Mod = new();
    private LiteralTerminal And = new();
    private LiteralTerminal Or = new();
    private LiteralTerminal AndAnd = new();
    private LiteralTerminal OrOr = new();
    private LiteralTerminal Caret = new();
    private LiteralTerminal Not = new();
    private LiteralTerminal Tilde = new();
    private LiteralTerminal Equal = new();
    private LiteralTerminal NotEqual = new();
    private LiteralTerminal Less = new();
    private LiteralTerminal LessEqual = new();
    private LiteralTerminal Greater = new();
    private LiteralTerminal GreaterEqual = new();
    private LiteralTerminal Question = new();
    private LiteralTerminal Colon = new();
    private LiteralTerminal ColonColon = new();
    private LiteralTerminal Semi = new();
    private LiteralTerminal Comma = new();
    private LiteralTerminal Assign = new();
    private LiteralTerminal StarAssign = new();
    private LiteralTerminal DivAssign = new();
    private LiteralTerminal ModAssign = new();
    private LiteralTerminal PlusAssign = new();
    private LiteralTerminal MinusAssign = new();
    private LiteralTerminal LeftShiftAssign = new();
    private LiteralTerminal RightShiftAssign = new();
    private LiteralTerminal AndAssign = new();
    private LiteralTerminal XorAssign = new();
    private LiteralTerminal OrAssign = new();

    private LiteralTerminal Dot = new();
    private LiteralTerminal True = new();
    private LiteralTerminal False = new();
    private AlternativeParser PreprocessorDirectiveName =  new();

    private LiteralTerminal Stream = new(){Name = "Stream"};
    private LiteralTerminal Stage = new(){Name = "Stage"};
    

    public void CreateTokens()
    {
        WS = WhiteSpace;
        Space = WhiteSpace | Eol;
        Spaces = Space.Optional().Repeat();
        SpacesWithLineBreak = WhiteSpace.Optional().Repeat().Then(Eol);
        AppendStructuredBuffer = Literal("AppendStructuredBuffer");
        ComponentNumber = Literal("1") | "2" | "3" | "4";
    
        Bool = Literal("bool");
        BoolVec.Add(Bool,ComponentNumber);
        BoolMat.Add(BoolVec,Literal("x"),ComponentNumber);
        Uint.Add("uint","unsigned int", "dword");
        UintVec.Add(Uint,ComponentNumber);
        UintMat.Add(UintVec,"x",ComponentNumber);
        Int = Literal("int");
        IntVec.Add(Int,ComponentNumber);
        IntMat.Add(IntVec,"x",ComponentNumber);
    
        Half = Literal("half");
        HalfVec.Add(Half, ComponentNumber);
        HalfMat.Add(HalfVec, "x", ComponentNumber);
        Float = Literal("float");
        FloatVec.Add(Float,ComponentNumber);
        FloatMat.Add(FloatVec,"x",ComponentNumber);
        Double = Literal("double");
        DoubleVec.Add(Double,ComponentNumber);
        DoubleMat.Add(DoubleVec,"x",ComponentNumber);
        Buffer = Literal("Buffer");
        ByteAddressBuffer = Literal("ByteAddressBuffer");
        Break = Literal("break");
        Case = Literal("case");
        CBuffer = Literal("cbuffer");
        Centroid = Literal("centroid");
        Class = Literal("class");
        ColumnMajor = Literal("column_major");
        Const = Literal("const");
        ConsumeStructuredBuffer = Literal("ConsumeStructuredBuffer");
        Continue = Literal("continue");
        Default = Literal("default");
        Discard = Literal("discard");
        Do = Literal("do");
        Else = Literal("else");
        Extern = Literal("extern");
        For = Literal("for");
        Groupshared = Literal("groupshared");
        If = Literal("if");
        In = Literal("in");
        Inout = Literal("inout") | "in out";
        InputPatch = Literal("InputPatch");
        Interface = Literal("interface");
        Line_ = Literal("line");
        LineAdj = Literal("lineadj");
        Linear = Literal("linear");
        LineStream = Literal("LineStream");
        Long = Literal("long");
        Matrix = Literal("matrix");
        Nointerpolation = Literal("nointerpolation");
        Noperspective = Literal("noperspective");
        Out = Literal("out");
        OutputPatch = Literal("OutputPatch");
        Packoffset = Literal("packoffset");
        Point = Literal("point");
        PointStream = Literal("PointStream");
        Precise = Literal("precise");
        Register = Literal("register");
        Return = Literal("return");
        RowMajor = Literal("row_major");
        RWBuffer = Literal("RWBuffer");
        RWByteAddressBuffer = Literal("RWByteAddressBuffer");
        RWStructuredBuffer = Literal("RWStructuredBuffer");
        Sample = Literal("sample");
        Sampler = Literal("sampler");
        SamplerComparisonState = Literal("SamplerComparisonState");
        SamplerState = Literal("SamplerState");
        Shared = Literal("shared");
        Static = Literal("static");
        Struct = Literal("struct");
        StructuredBuffer = Literal("StructuredBuffer");
        Switch = Literal("switch");
        TextureTypes.Add(
            Literal("Texture").NotFollowedBy("2DMS").Then(Literal("1") | "2" | "3").Then("D").Then(Literal("Array").Optional()),
            Literal("Texture2DMS").Then(Literal("Array").Optional()),
            Literal("TextureCube").Then(Literal("Array").Optional())
        );
        Triangle = Literal("triangle");
        TriangleAdj = Literal("triangleadj");
        TriangleStream = Literal("TriangleStream");
        Uniform = Literal("uniform");
        Vector = Literal("vector");
        Volatile = Literal("volatile");
        Void = Literal("void");
        While = Literal("while");
        LeftParen =  Literal("(");
        RightParen = Literal(")");
        LeftBracket = Literal("[");
        RightBracket = Literal("]");
        LeftBrace = Literal("{");
        RightBrace = Literal("}");

        LeftShift = Literal("<<");
        RightShift = Literal(">>");
        Plus = Literal("+");
        PlusPlus = Literal("++");
        Minus = Literal("-");
        MinusMinus = Literal("--");
        Star = Literal("*");
        Div = Literal("/");
        Mod = Literal("%");
        And = Literal("&");
        Or = Literal("|");
        AndAnd = Literal("&&");
        OrOr = Literal("||");
        Caret = Literal("^");
        Not = Literal("!");
        Tilde = Literal("~");
        Equal = Literal("==");
        NotEqual = Literal("!=");
        Less = Literal("<");
        LessEqual = Literal("<=");
        Greater = Literal(">");
        GreaterEqual = Literal(">=");
        Question = Literal("?");
        Colon = Literal(":");
        ColonColon = Literal("::");
        Semi = Literal(";");
        Comma = Literal(",");
        Assign = Literal("=");
        StarAssign = Literal("*=");
        DivAssign = Literal("/=");
        ModAssign = Literal("%=");
        PlusAssign = Literal("+=");
        MinusAssign = Literal("-=");
        LeftShiftAssign = Literal("<<=");
        RightShiftAssign = Literal(">>=");
        AndAssign = Literal("&=");
        XorAssign = Literal("^=");
        OrAssign = Literal("|=");

        Dot = Literal(".");
        True = Literal("true");
        False = Literal("false");
        PreprocessorDirectiveName.Add(  
            Literal("define"),
            Literal("elif"),
            Literal("else"),
            Literal("endif"),
            Literal("error"),
            Literal("if"),
            Literal("ifdef"),
            Literal("ifndef"),
            Literal("include"),
            Literal("line"),
            Literal("pragma"),
            Literal("undef")
        );
        
        Stage = Literal("stage");
        Stream = Literal("stream");
        
    }
}

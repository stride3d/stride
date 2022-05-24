using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace Stride.Shader.Parsing.Grammars.SDSL;

public partial class SDSLGrammar : Grammar
{
    protected CharTerminal WS;
    protected AlternativeParser Space =  new();
    protected RepeatParser Spaces = new();
    protected SequenceParser SpacesWithLineBreak =  new();
    protected LiteralTerminal AppendStructuredBuffer = new();
    protected AlternativeParser ComponentNumber =  new();
    
    protected LiteralTerminal Bool = new();
    protected SequenceParser BoolVec =  new();
    protected SequenceParser BoolMat =  new();
    protected AlternativeParser Uint =  new();
    protected SequenceParser UintVec =  new();
    protected SequenceParser UintMat =  new();
    protected LiteralTerminal Int = new();
    protected SequenceParser IntVec =  new();
    protected SequenceParser IntMat =  new();
    
    protected LiteralTerminal Half = new();
    protected SequenceParser HalfVec =  new();
    protected SequenceParser HalfMat =  new();
    protected LiteralTerminal Float = new();
    protected SequenceParser FloatVec =  new();
    protected SequenceParser FloatMat =  new();
    protected LiteralTerminal Double = new();
    protected SequenceParser DoubleVec =  new();
    protected SequenceParser DoubleMat =  new();
    protected LiteralTerminal Buffer = new();
    protected LiteralTerminal ByteAddressBuffer = new();
    protected LiteralTerminal Break = new();
    protected LiteralTerminal Case = new();
    protected LiteralTerminal CBuffer = new();
    protected LiteralTerminal Centroid = new();
    protected LiteralTerminal Class = new();
    protected LiteralTerminal ColumnMajor = new();
    protected LiteralTerminal Const = new();
    protected LiteralTerminal ConsumeStructuredBuffer = new();
    protected LiteralTerminal Continue = new();
    protected LiteralTerminal Default = new();
    protected LiteralTerminal Discard = new();
    protected LiteralTerminal Do = new();
    protected LiteralTerminal Else = new();
    protected LiteralTerminal Extern = new();
    protected LiteralTerminal For = new();
    protected LiteralTerminal Groupshared = new();
    protected LiteralTerminal If = new();
    protected LiteralTerminal In = new();
    protected AlternativeParser Inout =  new();
    protected LiteralTerminal InputPatch = new();
    protected LiteralTerminal Interface = new();

    public LiteralTerminal Line_ { get; protected set; }

    protected LiteralTerminal LineAdj = new();
    protected LiteralTerminal Linear = new();
    protected LiteralTerminal LineStream = new();
    protected LiteralTerminal Long = new();
    protected LiteralTerminal Matrix = new();
    protected LiteralTerminal Nointerpolation = new();
    protected LiteralTerminal Noperspective = new();
    protected LiteralTerminal Out = new();
    protected LiteralTerminal OutputPatch = new();
    protected LiteralTerminal Packoffset = new();
    protected LiteralTerminal Point = new();
    protected LiteralTerminal PointStream = new();
    protected LiteralTerminal Precise = new();
    protected LiteralTerminal Register = new();
    protected LiteralTerminal Return = new();
    protected LiteralTerminal RowMajor = new();
    protected LiteralTerminal RWBuffer = new();
    protected LiteralTerminal RWByteAddressBuffer = new();
    protected LiteralTerminal RWStructuredBuffer = new();
    protected LiteralTerminal Sample = new();
    protected LiteralTerminal Sampler = new();
    protected LiteralTerminal SamplerComparisonState = new();
    protected LiteralTerminal SamplerState = new();
    protected LiteralTerminal Shared = new();
    protected LiteralTerminal StaticConst = new();
    protected LiteralTerminal Static = new();
    protected LiteralTerminal Struct = new();
    protected LiteralTerminal StructuredBuffer = new();
    protected LiteralTerminal Switch = new();
    protected AlternativeParser TextureTypes = new();
    protected LiteralTerminal Triangle = new();
    protected LiteralTerminal TriangleAdj = new();
    protected LiteralTerminal TriangleStream = new();
    protected LiteralTerminal Uniform = new();
    protected LiteralTerminal Vector = new();
    protected LiteralTerminal Volatile = new();
    protected LiteralTerminal Void = new();
    protected LiteralTerminal While = new();
    protected LiteralTerminal LeftParen = new();
    protected LiteralTerminal RightParen = new();
    protected LiteralTerminal LeftBracket = new();
    protected LiteralTerminal RightBracket = new();
    protected LiteralTerminal LeftBrace = new();
    protected LiteralTerminal RightBrace = new();

    protected LiteralTerminal LeftShift = new();
    protected LiteralTerminal RightShift = new();
    protected LiteralTerminal Plus = new();
    protected LiteralTerminal PlusPlus = new();
    protected LiteralTerminal Minus = new();
    protected LiteralTerminal MinusMinus = new();
    protected LiteralTerminal Star = new();
    protected LiteralTerminal Div = new();
    protected LiteralTerminal Mod = new();
    protected LiteralTerminal And = new();
    protected LiteralTerminal Or = new();
    protected LiteralTerminal AndAnd = new();
    protected LiteralTerminal OrOr = new();
    protected LiteralTerminal Caret = new();
    protected LiteralTerminal Not = new();
    protected LiteralTerminal Tilde = new();
    protected LiteralTerminal Equal = new();
    protected LiteralTerminal NotEqual = new();
    protected LiteralTerminal Less = new();
    protected LiteralTerminal LessEqual = new();
    protected LiteralTerminal Greater = new();
    protected LiteralTerminal GreaterEqual = new();
    protected LiteralTerminal Question = new();
    protected LiteralTerminal Colon = new();
    protected LiteralTerminal ColonColon = new();
    protected LiteralTerminal Semi = new();
    protected LiteralTerminal Comma = new();
    protected LiteralTerminal Assign = new();
    protected LiteralTerminal StarAssign = new();
    protected LiteralTerminal DivAssign = new();
    protected LiteralTerminal ModAssign = new();
    protected LiteralTerminal PlusAssign = new();
    protected LiteralTerminal MinusAssign = new();
    protected LiteralTerminal LeftShiftAssign = new();
    protected LiteralTerminal RightShiftAssign = new();
    protected LiteralTerminal AndAssign = new();
    protected LiteralTerminal XorAssign = new();
    protected LiteralTerminal OrAssign = new();

    protected LiteralTerminal Dot = new();
    protected LiteralTerminal True = new();
    protected LiteralTerminal False = new();
    protected AlternativeParser PreprocessorDirectiveName =  new();

    protected LiteralTerminal Stream = new(){Name = "Stream"};
    protected LiteralTerminal Stage = new(){Name = "Stage"};
    

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
        StaticConst = Literal("static const");
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

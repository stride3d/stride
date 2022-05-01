using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;
namespace Stride.Shader.Parser;
public partial class SDSLGrammar : Grammar
{
    private CharTerminal WS;
    private AlternativeParser Space =  new();
    private RepeatParser Spaces =  new();
    private SequenceParser SpacesWithLineBreak =  new();
    private LiteralTerminal AppendStructuredBuffer;
    private AlternativeParser ComponentNumber =  new();
    
    private LiteralTerminal Bool;
    private SequenceParser BoolVec =  new();
    private SequenceParser BoolMat =  new();
    private AlternativeParser Uint =  new();
    private SequenceParser UintVec =  new();
    private SequenceParser UintMat =  new();
    private LiteralTerminal Int;
    private SequenceParser IntVec =  new();
    private SequenceParser IntMat =  new();
    
    private LiteralTerminal Half;
    private SequenceParser HalfVec =  new();
    private SequenceParser HalfMat =  new();
    private LiteralTerminal Float;
    private SequenceParser FloatVec =  new();
    private SequenceParser FloatMat =  new();
    private LiteralTerminal Double;
    private SequenceParser DoubleVec =  new();
    private SequenceParser DoubleMat =  new();
    private LiteralTerminal Buffer;
    private LiteralTerminal ByteAddressBuffer;
    private LiteralTerminal Break;
    private LiteralTerminal Case;
    private LiteralTerminal CBuffer;
    private LiteralTerminal Centroid;
    private LiteralTerminal Class;
    private LiteralTerminal ColumnMajor;
    private LiteralTerminal Const;
    private LiteralTerminal ConsumeStructuredBuffer;
    private LiteralTerminal Continue;
    private LiteralTerminal Default;
    private LiteralTerminal Discard;
    private LiteralTerminal Do;
    private LiteralTerminal Else;
    private LiteralTerminal Extern;
    private LiteralTerminal For;
    private LiteralTerminal Groupshared;
    private LiteralTerminal If;
    private LiteralTerminal In;
    private AlternativeParser Inout =  new();
    private LiteralTerminal InputPatch;
    private LiteralTerminal Interface;

    public LiteralTerminal Line_ { get; private set; }

    private LiteralTerminal LineAdj;
    private LiteralTerminal Linear;
    private LiteralTerminal LineStream;
    private LiteralTerminal Long;
    private LiteralTerminal Matrix;
    private LiteralTerminal Nointerpolation;
    private LiteralTerminal Noperspective;
    private LiteralTerminal Out;
    private LiteralTerminal OutputPatch;
    private LiteralTerminal Packoffset;
    private LiteralTerminal Point;
    private LiteralTerminal PointStream;
    private LiteralTerminal Precise;
    private LiteralTerminal Register;
    private LiteralTerminal Return;
    private LiteralTerminal RowMajor;
    private LiteralTerminal RWBuffer;
    private LiteralTerminal RWByteAddressBuffer;
    private LiteralTerminal RWStructuredBuffer;
    private LiteralTerminal Sample;
    private LiteralTerminal Sampler;
    private LiteralTerminal SamplerComparisonState;
    private LiteralTerminal SamplerState;
    private LiteralTerminal Shared;
    private LiteralTerminal Static;
    private LiteralTerminal Struct;
    private LiteralTerminal StructuredBuffer;
    private LiteralTerminal Switch;
    private AlternativeParser TextureTypes;
    private LiteralTerminal Triangle;
    private LiteralTerminal TriangleAdj;
    private LiteralTerminal TriangleStream;
    private LiteralTerminal Uniform;
    private LiteralTerminal Vector;
    private LiteralTerminal Volatile;
    private LiteralTerminal Void;
    private LiteralTerminal While;
    private LiteralTerminal LeftParen;
    private LiteralTerminal RightParen;
    private LiteralTerminal LeftBracket;
    private LiteralTerminal RightBracket;
    private LiteralTerminal LeftBrace;
    private LiteralTerminal RightBrace;

    private LiteralTerminal LeftShift;
    private LiteralTerminal RightShift;
    private LiteralTerminal Plus;
    private LiteralTerminal PlusPlus;
    private LiteralTerminal Minus;
    private LiteralTerminal MinusMinus;
    private LiteralTerminal Star;
    private LiteralTerminal Div;
    private LiteralTerminal Mod;
    private LiteralTerminal And;
    private LiteralTerminal Or;
    private LiteralTerminal AndAnd;
    private LiteralTerminal OrOr;
    private LiteralTerminal Caret;
    private LiteralTerminal Not;
    private LiteralTerminal Tilde;
    private LiteralTerminal Equal;
    private LiteralTerminal NotEqual;
    private LiteralTerminal Less;
    private LiteralTerminal LessEqual;
    private LiteralTerminal Greater;
    private LiteralTerminal GreaterEqual;
    private LiteralTerminal Question;
    private LiteralTerminal Colon;
    private LiteralTerminal ColonColon;
    private LiteralTerminal Semi;
    private LiteralTerminal Comma;
    private LiteralTerminal Assign;
    private LiteralTerminal StarAssign;
    private LiteralTerminal DivAssign;
    private LiteralTerminal ModAssign;
    private LiteralTerminal PlusAssign;
    private LiteralTerminal MinusAssign;
    private LiteralTerminal LeftShiftAssign;
    private LiteralTerminal RightShiftAssign;
    private LiteralTerminal AndAssign;
    private LiteralTerminal XorAssign;
    private LiteralTerminal OrAssign;

    private LiteralTerminal Dot;
    private LiteralTerminal True;
    private LiteralTerminal False;
    private AlternativeParser PreprocessorDirectiveName =  new();

    public void CreateTokens()
    {
        WS = WhiteSpace;
        Space = WhiteSpace | Eol;
        Spaces = Space.Optional().Repeat();
        SpacesWithLineBreak = WhiteSpace.Optional().Repeat().Then(Eol);
        AppendStructuredBuffer = Literal("AppendStructuredBuffer");
        ComponentNumber = Literal("1") | "2" | "3" | "4";
    
        Bool = Literal("bool");
        BoolVec = Bool.Then(ComponentNumber);
        BoolMat = BoolVec.Then("x").Then(ComponentNumber);
        Uint = Literal("uint") | "unsigned int" | "dword";
        UintVec = Uint.Then(ComponentNumber);
        UintMat = UintVec.Then("x").Then(ComponentNumber);
        Int = Literal("int");
        IntVec = Int.Then(ComponentNumber);
        IntMat = IntVec.Then("x").Then(ComponentNumber);
    
        Half = Literal("half");
        HalfVec = Half.Then(ComponentNumber);
        HalfMat = HalfVec.Then("x").Then(ComponentNumber);
        Float = Literal("float");
        FloatVec = Float.Then(ComponentNumber);
        FloatMat = FloatVec.Then("x").Then(ComponentNumber);
        Double = Literal("double");
        DoubleVec = Double.Then(ComponentNumber);
        DoubleMat = DoubleVec.Then("x").Then(ComponentNumber);
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
        TextureTypes =  
            (Literal("Texture").NotFollowedBy("2DMS").Then(Literal("1") | "2" | "3").Then("D").Then(Literal("Array").Optional()))
            | (Literal("Texture2DMS").Then(Literal("Array").Optional()))
            | (Literal("TextureCube").Then(Literal("Array").Optional()));
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
        PreprocessorDirectiveName =   
            Literal("define")
            |   Literal("elif")
            |   Literal("else")
            |   Literal("endif")
            |   Literal("error")
            |   Literal("if")
            |   Literal("ifdef")
            |   Literal("ifndef")
            |   Literal("include")
            |   Literal("line")
            |   Literal("pragma")
            |   Literal("undef");
        
    }
}

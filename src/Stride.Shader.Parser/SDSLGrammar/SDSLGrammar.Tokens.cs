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
    private CharTerminal AppendStructuredBuffer;
    private AlternativeParser ComponentNumber =  new();
    
    private CharTerminal Bool;
    private SequenceParser BoolVec =  new();
    private SequenceParser BoolMat =  new();
    private AlternativeParser Uint =  new();
    private SequenceParser UintVec =  new();
    private SequenceParser UintMat =  new();
    private CharTerminal Int;
    private SequenceParser IntVec =  new();
    private SequenceParser IntMat =  new();
    
    private CharTerminal Half;
    private SequenceParser HalfVec =  new();
    private SequenceParser HalfMat =  new();
    private CharTerminal Float;
    private SequenceParser FloatVec =  new();
    private SequenceParser FloatMat =  new();
    private CharTerminal Double;
    private SequenceParser DoubleVec =  new();
    private SequenceParser DoubleMat =  new();
    private CharTerminal Buffer;
    private CharTerminal ByteAddressBuffer;
    private CharTerminal Break;
    private CharTerminal Case;
    private CharTerminal CBuffer;
    private CharTerminal Centroid;
    private CharTerminal Class;
    private CharTerminal ColumnMajor;
    private CharTerminal Const;
    private CharTerminal ConsumeStructuredBuffer;
    private CharTerminal Continue;
    private CharTerminal Default;
    private CharTerminal Discard;
    private CharTerminal Do;
    private CharTerminal Else;
    private CharTerminal Extern;
    private CharTerminal For;
    private CharTerminal Groupshared;
    private CharTerminal If;
    private CharTerminal In;
    private AlternativeParser Inout =  new();
    private CharTerminal InputPatch;
    private CharTerminal Interface;
    private CharTerminal Line_ = Set("line");
    private CharTerminal LineAdj;
    private CharTerminal Linear;
    private CharTerminal LineStream;
    private CharTerminal Long;
    private CharTerminal Matrix;
    private CharTerminal Nointerpolation;
    private CharTerminal Noperspective;
    private CharTerminal Out;
    private CharTerminal OutputPatch;
    private CharTerminal Packoffset;
    private CharTerminal Point;
    private CharTerminal PointStream;
    private CharTerminal Precise;
    private CharTerminal Register;
    private CharTerminal Return;
    private CharTerminal RowMajor;
    private CharTerminal RWBuffer;
    private CharTerminal RWByteAddressBuffer;
    private CharTerminal RWStructuredBuffer;
    private CharTerminal Sample;
    private CharTerminal Sampler;
    private CharTerminal SamplerComparisonState;
    private CharTerminal SamplerState;
    private CharTerminal Shared;
    private CharTerminal Static;
    private CharTerminal Struct;
    private CharTerminal StructuredBuffer;
    private CharTerminal Switch;
    private AlternativeParser TextureTypes =  
        (Set("Texture").NotFollowedBy("2DMS").Then(Set("1") | "2" | "3").Then("D").Then(Set("Array").Optional()))
        | (Set("Texture2DMS").Then(Set("Array").Optional()))
        | (Set("TextureCube").Then(Set("Array").Optional()));
    private CharTerminal Triangle;
    private CharTerminal TriangleAdj;
    private CharTerminal TriangleStream;
    private CharTerminal Uniform;
    private CharTerminal Vector;
    private CharTerminal Volatile;
    private CharTerminal Void;
    private CharTerminal While;
    private CharTerminal LeftParen;
    private CharTerminal RightParen;
    private CharTerminal LeftBracket;
    private CharTerminal RightBracket;
    private CharTerminal LeftBrace;
    private CharTerminal RightBrace;

    private CharTerminal LeftShift;
    private CharTerminal RightShift;
    private CharTerminal Plus;
    private CharTerminal PlusPlus;
    private CharTerminal Minus;
    private CharTerminal MinusMinus;
    private CharTerminal Star;
    private CharTerminal Div;
    private CharTerminal Mod;
    private CharTerminal And;
    private CharTerminal Or;
    private CharTerminal AndAnd;
    private CharTerminal OrOr;
    private CharTerminal Caret;
    private CharTerminal Not;
    private CharTerminal Tilde;
    private CharTerminal Equal;
    private CharTerminal NotEqual;
    private CharTerminal Less;
    private CharTerminal LessEqual;
    private CharTerminal Greater;
    private CharTerminal GreaterEqual;
    private CharTerminal Question;
    private CharTerminal Colon;
    private CharTerminal ColonColon;
    private CharTerminal Semi;
    private CharTerminal Comma;
    private CharTerminal Assign;
    private CharTerminal StarAssign;
    private CharTerminal DivAssign;
    private CharTerminal ModAssign;
    private CharTerminal PlusAssign;
    private CharTerminal MinusAssign;
    private CharTerminal LeftShiftAssign;
    private CharTerminal RightShiftAssign;
    private CharTerminal AndAssign;
    private CharTerminal XorAssign;
    private CharTerminal OrAssign;

    private CharTerminal Dot;
    private CharTerminal True;
    private CharTerminal False;
    private AlternativeParser PreprocessorDirectiveName =  new();

    public void CreateTokens()
    {
        WS = WhiteSpace;
        Space = WhiteSpace | Eol;
        Spaces = Space.Optional().Repeat();
        SpacesWithLineBreak = WhiteSpace.Optional().Repeat().Then(Eol);
        AppendStructuredBuffer = Set("AppendStructuredBuffer");
        ComponentNumber = Set("1") | "2" | "3" | "4";
    
        Bool = Set("bool");
        BoolVec = Bool.Then(ComponentNumber);
        BoolMat = BoolVec.Then("x").Then(ComponentNumber);
        Uint = Set("uint") | "unsigned int" | "dword";
        UintVec = Uint.Then(ComponentNumber);
        UintMat = UintVec.Then("x").Then(ComponentNumber);
        Int = Set("int");
        IntVec = Int.Then(ComponentNumber);
        IntMat = IntVec.Then("x").Then(ComponentNumber);
    
        Half = Set("half");
        HalfVec = Half.Then(ComponentNumber);
        HalfMat = HalfVec.Then("x").Then(ComponentNumber);
        Float = Set("float");
        FloatVec = Float.Then(ComponentNumber);
        FloatMat = FloatVec.Then("x").Then(ComponentNumber);
        Double = Set("double");
        DoubleVec = Double.Then(ComponentNumber);
        DoubleMat = DoubleVec.Then("x").Then(ComponentNumber);
        Buffer = Set("Buffer");
        ByteAddressBuffer = Set("ByteAddressBuffer");
        Break = Set("break");
        Case = Set("case");
        CBuffer = Set("cbuffer");
        Centroid = Set("centroid");
        Class = Set("class");
        ColumnMajor = Set("column_major");
        Const = Set("const");
        ConsumeStructuredBuffer = Set("ConsumeStructuredBuffer");
        Continue = Set("continue");
        Default = Set("default");
        Discard = Set("discard");
        Do = Set("do");
        Else = Set("else");
        Extern = Set("extern");
        For = Set("for");
        Groupshared = Set("groupshared");
        If = Set("if");
        In = Set("in");
        Inout = Set("inout") | "in out";
        InputPatch = Set("InputPatch");
        Interface = Set("interface");
        Line_ = Set("line");
        LineAdj = Set("lineadj");
        Linear = Set("linear");
        LineStream = Set("LineStream");
        Long = Set("long");
        Matrix = Set("matrix");
        Nointerpolation = Set("nointerpolation");
        Noperspective = Set("noperspective");
        Out = Set("out");
        OutputPatch = Set("OutputPatch");
        Packoffset = Set("packoffset");
        Point = Set("point");
        PointStream = Set("PointStream");
        Precise = Set("precise");
        Register = Set("register");
        Return = Set("return");
        RowMajor = Set("row_major");
        RWBuffer = Set("RWBuffer");
        RWByteAddressBuffer = Set("RWByteAddressBuffer");
        RWStructuredBuffer = Set("RWStructuredBuffer");
        Sample = Set("sample");
        Sampler = Set("sampler");
        SamplerComparisonState = Set("SamplerComparisonState");
        SamplerState = Set("SamplerState");
        Shared = Set("shared");
        Static = Set("static");
        Struct = Set("struct");
        StructuredBuffer = Set("StructuredBuffer");
        Switch = Set("switch");
        TextureTypes =  
            (Set("Texture").NotFollowedBy("2DMS").Then(Set("1") | "2" | "3").Then("D").Then(Set("Array").Optional()))
            | (Set("Texture2DMS").Then(Set("Array").Optional()))
            | (Set("TextureCube").Then(Set("Array").Optional()));
        Triangle = Set("triangle");
        TriangleAdj = Set("triangleadj");
        TriangleStream = Set("TriangleStream");
        Uniform = Set("uniform");
        Vector = Set("vector");
        Volatile = Set("volatile");
        Void = Set("void");
        While = Set("while");
        LeftParen = Set("(");
        RightParen = Set(")");
        LeftBracket = Set("[");
        RightBracket = Set("]");
        LeftBrace = Set("{");
        RightBrace = Set("}");

        LeftShift = Set("<<");
        RightShift = Set(">>");
        Plus = Set("+");
        PlusPlus = Set("++");
        Minus = Set("-");
        MinusMinus = Set("--");
        Star = Set("*");
        Div = Set("/");
        Mod = Set("%");
        And = Set("&");
        Or = Set("|");
        AndAnd = Set("&&");
        OrOr = Set("||");
        Caret = Set("^");
        Not = Set("!");
        Tilde = Set("~");
        Equal = Set("==");
        NotEqual = Set("!=");
        Less = Set("<");
        LessEqual = Set("<=");
        Greater = Set(">");
        GreaterEqual = Set(">=");
        Question = Set("?");
        Colon = Set(":");
        ColonColon = Set("::");
        Semi = Set(";");
        Comma = Set(",");
        Assign = Set("=");
        StarAssign = Set("*=");
        DivAssign = Set("/=");
        ModAssign = Set("%=");
        PlusAssign = Set("+=");
        MinusAssign = Set("-=");
        LeftShiftAssign = Set("<<=");
        RightShiftAssign = Set(">>=");
        AndAssign = Set("&=");
        XorAssign = Set("^=");
        OrAssign = Set("|=");

        Dot = Set(".");
        True = Set("true");
        False = Set("false");
        PreprocessorDirectiveName =   
            Set("define")
            |   "elif"
            |   "else"
            |   "endif"
            |   "error"
            |   "if"
            |   "ifdef"
            |   "ifndef"
            |   "include"
            |   "line"
            |   "pragma"
            |   "undef";
        
    }
}

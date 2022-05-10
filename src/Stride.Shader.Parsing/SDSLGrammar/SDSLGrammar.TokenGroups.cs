using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace Stride.Shader.Parsing;
public partial class SDSLGrammar : Grammar
{
    public AlternativeParser IncOperators = new();

    public AlternativeParser Operators = new();  
    public AlternativeParser AssignOperators = new();

    public AlternativeParser BoolTypes = new();

    public AlternativeParser HalfTypes = new();

    public AlternativeParser FloatTypes = new();

    public AlternativeParser DoubleTypes = new();

    public AlternativeParser IntTypes = new();

    public AlternativeParser UintTypes = new();

    public AlternativeParser ValueTypes = new();
    public AlternativeParser StorageFlag = new();

    public AlternativeParser Keywords = new();
    
    public void CreateTokenGroups()
    {
        IncOperators.Add(
            PlusPlus,
            MinusMinus
        );

        Operators.Add(
            Plus,
            Minus,
            Star,
            Div,
            Mod,
            LeftShift,
            RightShift
        );
        
        AssignOperators.Add(
            Assign,
            StarAssign,
            DivAssign,
            ModAssign,
            PlusAssign,
            MinusAssign,
            LeftShiftAssign,
            RightShiftAssign,
            AndAssign,
            XorAssign,
            OrAssign
        );

        BoolTypes.Add(
            Bool.NotFollowedBy(Set("1234")),
            BoolVec.NotFollowedBy("x"),
            BoolMat
        );

        HalfTypes.Add(
            Half.NotFollowedBy(Set("1234")),
            HalfVec.NotFollowedBy("x"),
            HalfMat
        );

        FloatTypes.Add(
            Float.NotFollowedBy(Set("1234")),
            FloatVec.NotFollowedBy("x"),
            FloatMat
        );

        DoubleTypes.Add(
            Double.NotFollowedBy(Set("1234")),
            DoubleVec.NotFollowedBy("x"),
            DoubleMat
        );

        IntTypes.Add(
            Int.NotFollowedBy(Set("1234")),
            IntVec.NotFollowedBy("x"),
            IntMat
        );

        UintTypes.Add(
            Uint.NotFollowedBy(Set("1234")),
            UintVec.NotFollowedBy("x"),
            UintMat
        );

        ValueTypes.Add(
            BoolTypes,
            HalfTypes,
            FloatTypes,
            DoubleTypes,
            IntTypes,
            UintTypes
        );

        Keywords.Add(
            AppendStructuredBuffer,
            Buffer,
            ByteAddressBuffer,
            Break,
            Case,
            CBuffer,
            Centroid,
            Class,
            ColumnMajor, 
            Const, 
            ConsumeStructuredBuffer, 
            Continue,
            Default, 
            Discard,
            Do,
            Else,
            Extern,
            For,
            Groupshared,
            If,
            In,
            Inout,
            InputPatch,
            Interface,
            Line_,
            LineAdj,
            Linear,
            LineStream,
            Matrix,
            Nointerpolation,
            Noperspective,
            Out,
            OutputPatch,
            Packoffset,
            Point,
            PointStream,
            Precise,
            Register,
            Return,
            RowMajor,
            RWBuffer,
            RWByteAddressBuffer,
            RWStructuredBuffer,
            Sample,
            Sampler,
            SamplerComparisonState,
            SamplerState,
            Shared,
            Stage,
            Stream,
            Static,
            Struct,
            StructuredBuffer,
            Switch,
            TextureTypes,
            Triangle,
            TriangleAdj,
            TriangleStream,
            Uniform,
            ValueTypes,
            Vector,
            Volatile,
            Void,
            While
        );

        StorageFlag.Add(
            Literal("constant"),
            RowMajor,
            ColumnMajor,
            Extern,
            Precise,
            Shared,
            Groupshared,
            Static,
            Uniform,
            Volatile,
            Linear,
            Centroid,
            Nointerpolation,
            Noperspective,
            Sample,
            In,
            Out,
            Inout,
            Point,
            Line_,
            Triangle,
            LineAdj,
            TriangleAdj
        );
        
    }
}
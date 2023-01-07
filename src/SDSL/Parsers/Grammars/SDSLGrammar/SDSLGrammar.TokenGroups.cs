using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace SDSL.Parsing.Grammars.SDSL;

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

    public AlternativeParser SimpleTypes = new() { Name = "ValueTypes"};
    public SequenceParser ArrayTypes = new() { Name = "ArrayTypes"};
    public AlternativeParser ValueTypes = new() { Name = "ValueTypes"};
    public AlternativeParser StorageFlag = new();

    public AlternativeParser Keywords = new();
    
    public void CreateTokenGroups()
    {
        IncOperators.Add(
            PlusPlus,
            MinusMinus
        );

        Operators.Add(
            PlusPlus,
            Plus,
            MinusMinus,
            Minus,
            Star,
            Div,
            Mod,
            LeftShift,
            RightShift,
            AndAnd,
            And,
            OrOr,
            Or,
            "^",
            Equal,
            "==",
            NotEqual,
            Question

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
            Bool.NotFollowedBy(Set("1234")).Named("BoolScalar"),
            BoolVec.NotFollowedBy("x").Named("BoolVec"),
            BoolMat.Named("BoolMatrix")
        );

        HalfTypes.Add(
            Half.NotFollowedBy(Set("1234")).Named("HalfScalar"),
            HalfVec.NotFollowedBy("x").Named("HalfVec"),
            HalfMat.Named("HalfMatrix")
        );

        FloatTypes.Add(
            Float.NotFollowedBy(Set("1234")).Named("FloatScalar"),
            FloatVec.NotFollowedBy("x").Named("FloatVec"),
            FloatMat.Named("FloatMatrix")
        );

        DoubleTypes.Add(
            Double.NotFollowedBy(Set("1234")).Named("DoubleScalar"),
            DoubleVec.NotFollowedBy("x").Named("DoubleVec"),
            DoubleMat.Named("DoubleMatrix")
        );

        IntTypes.Add(
            Int.NotFollowedBy(Set("1234")).Named("IntScalar"),
            IntVec.NotFollowedBy("x").Named("IntVec"),
            IntMat.Named("IntMatrix")
        );

        UintTypes.Add(
            Uint.NotFollowedBy(Set("1234")).Named("UintScalar"),
            UintVec.NotFollowedBy("x").Named("UintVec"),
            UintMat.Named("UintMatrix")
        );

        SimpleTypes.Add(
            BoolTypes,
            HalfTypes,
            FloatTypes,
            DoubleTypes,
            IntTypes,
            UintTypes,
            BufferTypes,
            TextureTypes,
            Void,
            Identifier.Named("UserDefined")
        );

        ArrayTypes.Add(
            SimpleTypes,
            LeftBracket,
            RightBracket
        );
        ArrayTypes.Separator = WhiteSpace.Repeat(0);

        ValueTypes.Add(
            ArrayTypes,
            SimpleTypes
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
            StaticConst,
            Static,
            Struct,
            StructuredBuffer,
            Switch,
            TextureBase,
            Triangle,
            TriangleAdj,
            TriangleStream,
            Uniform,
            SimpleTypes,
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
            StaticConst,
            Static,
            Uniform,
            Volatile,
            Linear,
            Centroid,
            Nointerpolation,
            Noperspective,
            Sample,
            In.NotFollowedBy(WhiteSpace.Repeat(0) & Out),
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
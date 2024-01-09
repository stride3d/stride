using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;
using static SDSL.Parsing.Grammars.CommonParsers;

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
            Caret,
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

        
        SimpleTypes.Add(
            BuiltinNumericTypes,
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
            CommonParsers.Buffer,
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
            CommonParsers.Stream,
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
            CommonParsers.Volatile,
            CommonParsers.Void,
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
            CommonParsers.Volatile,
            Linear,
            Centroid,
            Nointerpolation,
            Noperspective,
            Sample,
            Inout,
            In.NotFollowedBy(WhiteSpace.Repeat(0) & Out),
            Out,
            Point,
            Triangle,
            LineAdj,
            TriangleAdj
        );
        
    }
}
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Text;
// using System.Threading.Tasks;
// using Spv.Generator;
// using SDSL.Parsing.AST.Shader;
// using SDSL.Parsing.Grammars.NativeType;
// using static Spv.Specification;

// namespace SDSL.Spirv;

// public partial class SpirvEmitter : Module
// {

//     public Instruction AsSpvType(string n)
//     {
//         var match = NativeTypeGrammar.ParseNativeType(n);
//         if (!match.HasMatches) return TryGetUserDefined(n);
//         if (!match["TypeParser"].HasMatches) return TryGetUserDefined(n);
//         else return match["TypeParser"].Matches[0] switch
//         {
//             { Name: "Bool" } => TypeBool(),
//             { Name: "Byte" } => TypeInt(8, 0),
//             { Name: "SByte" } => TypeInt(8, 1),
//             { Name: "UShort" } => TypeInt(16, 0),
//             { Name: "Short" } => TypeInt(16, 1),
//             { Name: "Half" } => TypeFloat(16),
//             { Name: "UInt" } => TypeInt(32, 0),
//             { Name: "Int" } => TypeInt(32, 1),
//             { Name: "Float" } => TypeFloat(32),
//             { Name: "ULong" } => TypeInt(64, 0),
//             { Name: "Long" } => TypeInt(64, 1),
//             { Name: "Double" } => TypeInt(32, 1),


//             { Name: "BoolVector" } m => TypeVector(TypeBool(), (int)m["RowCount"].Value),
//             { Name: "ByteVector" } m => TypeVector(TypeInt(8, 0), (int)m["RowCount"].Value),
//             { Name: "SByteVector" } m => TypeVector(TypeInt(8, 1), (int)m["RowCount"].Value),
//             { Name: "UShortVector" } m => TypeVector(TypeInt(16, 0), (int)m["RowCount"].Value),
//             { Name: "ShortVector" } m => TypeVector(TypeInt(16, 1), (int)m["RowCount"].Value),
//             { Name: "HalfVector" } m => TypeVector(TypeFloat(16), (int)m["RowCount"].Value),
//             { Name: "UIntVector" } m => TypeVector(TypeInt(32, 0), (int)m["RowCount"].Value),
//             { Name: "IntVector" } m => TypeVector(TypeInt(32, 1), (int)m["RowCount"].Value),
//             { Name: "FloatVector" } m => TypeVector(TypeFloat(32), (int)m["RowCount"].Value),
//             { Name: "ULongVector" } m => TypeVector(TypeInt(64, 0), (int)m["RowCount"].Value),
//             { Name: "LongVector" } m => TypeVector(TypeInt(64, 1), (int)m["RowCount"].Value),
//             { Name: "DoubleVector" } m => TypeVector(TypeInt(32, 1), (int)m["RowCount"].Value),

//             { Name: "BoolMatrix" } m => TypeMatrix(TypeVector(TypeBool(), (int)m["RowCount"].Value), (int)m["ColCount"].Value),
//             { Name: "ByteMatrix" } m => TypeMatrix(TypeVector(TypeInt(8, 0), (int)m["RowCount"].Value), (int)m["ColCount"].Value),
//             { Name: "SByteMatrix" } m => TypeMatrix(TypeVector(TypeInt(8, 1), (int)m["RowCount"].Value), (int)m["ColCount"].Value),
//             { Name: "UShortMatrix" } m => TypeMatrix(TypeVector(TypeInt(16, 0), (int)m["RowCount"].Value), (int)m["ColCount"].Value),
//             { Name: "ShortMatrix" } m => TypeMatrix(TypeVector(TypeInt(16, 1), (int)m["RowCount"].Value), (int)m["ColCount"].Value),
//             { Name: "HalfMatrix" } m => TypeMatrix(TypeVector(TypeFloat(16), (int)m["RowCount"].Value), (int)m["ColCount"].Value),
//             { Name: "UIntMatrix" } m => TypeMatrix(TypeVector(TypeInt(32, 0), (int)m["RowCount"].Value), (int)m["ColCount"].Value),
//             { Name: "IntMatrix" } m => TypeMatrix(TypeVector(TypeInt(32, 1), (int)m["RowCount"].Value), (int)m["ColCount"].Value),
//             { Name: "FloatMatrix" } m => TypeMatrix(TypeVector(TypeFloat(32), (int)m["RowCount"].Value), (int)m["ColCount"].Value),
//             { Name: "ULongMatrix" } m => TypeMatrix(TypeVector(TypeInt(64, 0), (int)m["RowCount"].Value), (int)m["ColCount"].Value),
//             { Name: "LongMatrix" } m => TypeMatrix(TypeVector(TypeInt(64, 1), (int)m["RowCount"].Value), (int)m["ColCount"].Value),
//             { Name: "DoubleMatrix" } m => TypeMatrix(TypeVector(TypeInt(32, 1), (int)m["RowCount"].Value), (int)m["ColCount"].Value),
//             _ => throw new NotImplementedException()
//         };
//     }

//     public Instruction? TryGetUserDefined(string n)
//     {
//         if (ShaderTypes.TryGetValue(n, out var i))
//             return i.SpvType;
//         else return null;
//     }

//     void CreateStructTypes()
//     {

//     }
// }

// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core.Shaders.Utility
{
    public partial class MessageCode
    {
        public readonly string Code;

        public readonly string Text;

        public MessageCode(string text)
        {
            Code = "";
            Text = text;
        }

        public MessageCode(string code, string text)
        {
            Code = code;
            Text = text;
        }

        public static implicit operator MessageCode(string text)
        {
            return new MessageCode(text);
        }

        #region Static members

        // Warnings
        public static readonly MessageCode WarningUnknown                           = new MessageCode("W0000", "Unknown warning");

        public static readonly MessageCode WarningTypeAsConstructor                 = new MessageCode("W0001", "Invalid type used as a constructor [{0}]");
        public static readonly MessageCode WarningTypeInferenceUnknownExpression    = new MessageCode("W0002", "Type inference for unknown expression is supported [{0}]");
        public static readonly MessageCode WarningNoTypeReferenceMember             = new MessageCode("W0003", "Unable to find type reference for member [{0}]");
        
        // Error
        public static readonly MessageCode ErrorAnalysisUnknown                     = new MessageCode("E0000", "Unknown analysis error");

        public static readonly MessageCode ErrorBinaryTypeDeduction                 = new MessageCode("E0001", "Can't deduce type of binary operation between [{0}] and [{1}]");
        public static readonly MessageCode ErrorScalarTypeConversion                = new MessageCode("E0002", "Unsupported scalar type conversion between [{0}] and [{1}]");
        public static readonly MessageCode ErrorIndexerType                         = new MessageCode("E0003", "Unable to find type for indexer: [{0}]");
        public static readonly MessageCode ErrorLiteralType                         = new MessageCode("E0004", "Unable to find type reference for literal value [{0}]");
        public static readonly MessageCode ErrorNoOverloadedMethod                  = new MessageCode("E0005", "Unable to find a suitable overloaded method [{0}]");
        public static readonly MessageCode ErrorNoReferencedMethod                  = new MessageCode("E0006", "Unable to find the referenced method [{0}]");
        public static readonly MessageCode ErrorNoTypeReferenceTypename             = new MessageCode("E0008", "Unable to find type reference for typename [{0}]");

        public static readonly MessageCode ErrorUnexpectedException                 = new MessageCode("E0009", "Unexpected exception: {0}");


        #endregion
    }
}

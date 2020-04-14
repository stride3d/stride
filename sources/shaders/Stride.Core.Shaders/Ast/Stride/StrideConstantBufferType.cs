// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Shaders.Ast.Hlsl;

namespace Xenko.Core.Shaders.Ast.Xenko
{
    public partial class XenkoConstantBufferType : ConstantBufferType
    {
        /// <summary>
        ///   Resource group keyword (rgroup).
        /// </summary>
        public static readonly XenkoConstantBufferType ResourceGroup = new XenkoConstantBufferType("rgroup");

        /// <summary>
        /// Initializes a new instance of the <see cref="XenkoStorageQualifier"/> class.
        /// </summary>
        public XenkoConstantBufferType()
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="XenkoStorageQualifier"/> class.
        /// </summary>
        /// <param name="key">
        /// The key.
        /// </param>
        public XenkoConstantBufferType(string key)
            : base(key)
        {
        }

        /// <summary>
        /// Parses the specified enum name.
        /// </summary>
        /// <param name="enumName">
        /// Name of the enum.
        /// </param>
        /// <returns>
        /// A qualifier
        /// </returns>
        public static new ConstantBufferType Parse(string enumName)
        {
            if (enumName == (string)ResourceGroup.Key)
                return ResourceGroup;

            return ConstantBufferType.Parse(enumName);
        }
    }
}

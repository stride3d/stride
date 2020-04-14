// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Shaders.Ast.Xenko;
using Xenko.Core.Shaders.Analysis.Hlsl;
using Xenko.Core.Shaders.Parser;

namespace Xenko.Shaders.Parser.Analysis
{
    internal class XenkoTypeAnalysis : HlslSemanticAnalysis
    {
        #region Contructor

        public XenkoTypeAnalysis(ParsingResult result)
            : base(result)
        {
            SetupHlslAnalyzer();
        }

        #endregion

        public void Run(ShaderClassType shaderClassType)
        {
            Visit(shaderClassType);
        }
    }
}

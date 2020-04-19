#region Header Licence
//  ---------------------------------------------------------------------
// 
//  Copyright (c) 2009 Alexandre Mutel and Microsoft Corporation.  
//  All rights reserved.
// 
//  This code module is part of NShader, a plugin for visual studio
//  to provide syntax highlighting for shader languages (hlsl, glsl, cg)
// 
//  ------------------------------------------------------------------
// 
//  This code is licensed under the Microsoft Public License. 
//  See the file License.txt for the license details.
//  More info on: http://nshader.codeplex.com
// 
//  ------------------------------------------------------------------
#endregion
using System.Collections.Generic;
using NShader.Lexer;

namespace NShader
{
    public class GLSLShaderTokenProvider : IShaderTokenProvider
    {
        private static EnumMap<ShaderToken> map;

        static GLSLShaderTokenProvider()
        {
            map = new EnumMap<ShaderToken>();
            map.Load("GLSLKeywords.map");
        }

        public ShaderToken GetTokenFromSemantics(string text)
        {
            text = text.Replace(" ", "");
            ShaderToken token;
            if (!map.TryGetValue(text.ToUpperInvariant(), out token))
            {
                token = ShaderToken.IDENTIFIER;
            }
            return token;
        }

        public ShaderToken GetTokenFromIdentifier(string text)
        {
            ShaderToken token;
            if (!map.TryGetValue(text, out token))
            {
                token = ShaderToken.IDENTIFIER;
            }
            return token;
        }
    }
}
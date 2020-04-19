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
using System.IO;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;

namespace NShader
{
    public class NShaderScannerFactory {
        private static NShaderScanner strideShaderScanner;
        private static Dictionary<string, NShaderScanner> mapExtensionToScanner;

        static void Init() {
            if (mapExtensionToScanner == null)
            {
                mapExtensionToScanner = new Dictionary<string, NShaderScanner>();

                // Stride Scanner
                strideShaderScanner = new NShaderScanner(new StrideShaderTokenProvider());

                foreach (var field in typeof (NShaderSupportedExtensions).GetFields())
                {
                    if (field.Name.StartsWith("Stride_"))
                        mapExtensionToScanner.Add(field.GetValue(null).ToString(), strideShaderScanner);
                }
            }
        }

        public static NShaderScanner GetShaderScanner(string filepath)
        {
            Init();

            string ext = Path.GetExtension(filepath).ToLowerInvariant();
            NShaderScanner scanner;
            if (!mapExtensionToScanner.TryGetValue(ext, out scanner))
            {
                scanner = strideShaderScanner;
            }
            return scanner;
        }

        public static NShaderScanner GetShaderScanner(IVsTextLines buffer)
        {
            return GetShaderScanner(FilePathUtilities.GetFilePath(buffer));
        }
    }
}
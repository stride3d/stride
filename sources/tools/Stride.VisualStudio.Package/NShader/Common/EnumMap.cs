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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace NShader
{

    public class EnumMap<T> : Dictionary<string, T>
    {
        public void Load(string resource)
        {
            Stream file = typeof(T).Assembly.GetManifestResourceStream(typeof(Stride.VisualStudio.Resources).Namespace + ".NShader.Common." + resource);
            TextReader textReader = new StreamReader(file);
            string line;
            while ((line = textReader.ReadLine()) != null )
            {
                int indexEqu = line.IndexOf('=');
                if ( indexEqu > 0 )
                {
                    string enumName = line.Substring(0, indexEqu);
                    string value = line.Substring(indexEqu + 1, line.Length - indexEqu-1).Trim();
                    string[] values = Regex.Split(value, @"[\t ]+");
                    T enumValue = (T)Enum.Parse(typeof(T), enumName);
                    foreach (string token in values)
                    {
                        if (!ContainsKey(token))
                        {
                            Add(token, enumValue);
                        } else
                        {
                            Trace.WriteLine(string.Format("Warning: token {0} for enum {1} already added for {2}", token, enumValue, this[token]));
                        }
                    }
                }

            }
        }
    }
}
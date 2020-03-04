// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Stride.Core.Assets
{
    public static class XenkoToStrideRenameHelper
    {
        public enum StrideContentType
        {
            Asset,
            Project,
            Code,
            Package,
        }

        public static string ReplaceStrideContent(string content, StrideContentType type)
        {
            // Rename various instances of Xenko to Stride
            switch (type)
            {
                case StrideContentType.Package:
                    break;
                case StrideContentType.Project:
                    content = Regex.Replace(content, "Include=\"Xenko", "Include=\"Stride");
                    break;
                case StrideContentType.Code:
                    content = Regex.Replace(content, @"using Xenko", "using Stride");
                    content = Regex.Replace(content, @"\bXenko\.", "Stride.");
                    break;
                case StrideContentType.Asset:
                    content = Regex.Replace(content, @"SerializedVersion: \{Xenko", "SerializedVersion: {Stride");
                    content = Regex.Replace(content, @"\!Xenko\.([^\s]+),Xenko\.([^\s]+)", "!Stride.$1,Stride.$2");
                    // xkeffectlog
                    content = Regex.Replace(content, @"EffectName: Xenko", "EffectName: Stride");
                    content = Regex.Replace(content, @"Name: XENKO_", "Name: STRIDE_");
                    content = Regex.Replace(content, @"XenkoEffectBase\.", "StrideEffectBase.");
                    break;
            }

            return content;
        }

        public static string RenameStrideFile(string filePath, StrideContentType type)
        {
            var fileContents = File.ReadAllText(filePath);
            var newFileContents = fileContents;

            // Rename namespaces
            newFileContents = ReplaceStrideContent(newFileContents, type);

            // Save file if there were any changes
            if (newFileContents != fileContents)
            {
                File.WriteAllText(filePath, newFileContents);
            }

            // Rename extension
            var extension = Path.GetExtension(filePath);
            if (extension.StartsWith(".xk"))
            {
                File.Move(filePath, filePath = filePath.Replace(".xk", ".sd"));
            }

            return filePath;
        }
    }
}

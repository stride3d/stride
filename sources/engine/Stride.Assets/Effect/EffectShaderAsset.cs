// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Stride.Core.Assets;
using Stride.Core;
using Stride.Shaders.Parser;
using Stride.Shaders.Parser.Mixins;

namespace Stride.Assets.Effect
{
    /// <summary>
    /// Describes a shader effect asset (sdsl).
    /// </summary>
    [DataContract("EffectShader")]
    [AssetDescription(FileExtension, AlwaysMarkAsRoot = true, AllowArchetype = false)]
    public sealed partial class EffectShaderAsset : ProjectSourceCodeWithFileGeneratorAsset
    {
        /// <summary>
        /// The default file extension used by the <see cref="EffectShaderAsset"/>.
        /// </summary>
        public const string FileExtension = ".sdsl";

        public static Regex Regex = new Regex(@"(^|\s)(class)($|\s)");

        public override string Generator => "StrideShaderKeyGenerator";

        public override void Save(Stream stream)
        {
            //regex the shader name if it has changed
            Text = Regex.Replace(Text, "$1shader$3");

            base.Save(stream);
        }

        public override void SaveGeneratedAsset(AssetItem assetItem)
        {
            var generatedFileData = ShaderKeyFileHelper.GenerateCode(assetItem.FullPath, Text);
            //generate the .sdsl.cs files
            File.WriteAllBytes(assetItem.GetGeneratedAbsolutePath(), generatedFileData);
        }
    }
}

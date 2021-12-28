// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Globalization;

namespace Stride.Core.Shaders.Convertor
{
    /// <summary>
    /// Describes a HLSL ShaderModel (SM2, SM3, SM4...etc.)
    /// </summary>
    public enum ShaderModel
    {
        /// <summary>
        /// SM 1.1
        /// </summary>
        Model11,

        /// <summary>
        /// SM 2.0
        /// </summary>
        Model20,

        /// <summary>
        /// SM 3.0
        /// </summary>
        Model30,

        /// <summary>
        /// SM 4.0
        /// </summary>
        Model40,

        /// <summary>
        /// SM 4.1
        /// </summary>
        Model41,

        /// <summary>
        /// SM 5.0
        /// </summary>
        Model50,
    }

    internal class ShaderModelHelper
    {
        /// <summary>
        /// Parses the specified short profile (4_0, 3_0, 5_0)
        /// </summary>
        /// <param name="profile">The profile.</param>
        /// <returns>ShaderModel.</returns>
        public static ShaderModel Parse(string profile)
        {
            var model = ShaderModel.Model30;

            switch (profile)
            {
                case "1_1":
                    model = ShaderModel.Model11;
                    break;
                case "2_0":
                    model = ShaderModel.Model20;
                    break;
                case "3_0":
                    model = ShaderModel.Model30;
                    break;
                case "4_0":
                    model = ShaderModel.Model40;
                    break;
                case "4_1":
                    model = ShaderModel.Model41;
                    break;
                case "5_0":
                    model = ShaderModel.Model50;
                    break;
            }

            return model;
        }

        /// <summary>
        /// Parses the specified full profile (vs_4_0) and output the stage as well.
        /// </summary>
        /// <param name="profile">The profile.</param>
        /// <param name="stage">The stage.</param>
        /// <returns>Return the ShaderModel default to 3.0 if not parsed correctly.</returns>
        public static ShaderModel Parse(string profile, out PipelineStage stage)
        {
            profile = CultureInfo.InvariantCulture.TextInfo.ToLower(profile);

            if (profile.StartsWith("vs"))
                stage = PipelineStage.Vertex;
            else if (profile.StartsWith("ps"))
                stage = PipelineStage.Pixel;
            else if (profile.StartsWith("gs"))
                stage = PipelineStage.Geometry;
            else if (profile.StartsWith("cs"))
                stage = PipelineStage.Compute;
            else if (profile.StartsWith("hs"))
                stage = PipelineStage.Hull;
            else if (profile.StartsWith("ds"))
                stage = PipelineStage.Domain;
            else
            {
                stage = PipelineStage.None;
            }

            return profile.Length > 4 ? Parse(profile.Substring(3)) : ShaderModel.Model30;
        }
    }
}

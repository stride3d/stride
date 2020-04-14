// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;
using Stride.Graphics;

namespace Stride.Shaders.Compiler
{
    [DataContract]
    public struct EffectCompilerParameters
    {
        public static readonly EffectCompilerParameters Default = new EffectCompilerParameters
        {
            Platform = GraphicsPlatform.Direct3D11,
            Profile = GraphicsProfile.Level_11_0,
            Debug = true,
            OptimizationLevel = 0,
        };

        public void ApplyCompilationMode(CompilationMode compilationMode)
        {
            switch (compilationMode)
            {
                case CompilationMode.Debug:
                case CompilationMode.Testing:
                    Debug = true;
                    OptimizationLevel = 0;
                    break;
                case CompilationMode.Release:
                    Debug = true;
                    OptimizationLevel = 1;
                    break;
                case CompilationMode.AppStore:
                    Debug = false;
                    OptimizationLevel = 2;
                    break;
            }
        }

        public GraphicsPlatform Platform { get; set; }

        public GraphicsProfile Profile { get; set; }

        public bool Debug { get; set; }

        public int OptimizationLevel { get; set; }

        /// <summary>
        /// Gets or sets the priority (in case this compile is scheduled in a custom async pool)
        /// </summary>
        /// <value>
        /// The priority.
        /// </value>
        [DataMemberIgnore]
        public int TaskPriority { get; set; }
    }
}

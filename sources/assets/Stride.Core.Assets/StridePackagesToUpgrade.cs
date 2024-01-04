using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Core.Assets;

public static class StridePackagesToUpgrade
{
    public static HashSet<string> PackageNames =
    [
        #region default packages in a Stride project
        "Stride.Engine",
        "Stride.Core.Assets.CompilerApp",
        "Stride.Navigation",
        "Stride.Particles",
        "Stride.Physics",
        "Stride.UI",
        "Stride.Video",
        #endregion
        "Stride",
        "Stride.Assets.Models",
        "Stride.Assets.Presentation",
        "Stride.Assimp",
        "Stride.ConnectionRouter",
        "Stride.Core",
        "Stride.Core.Assets",
        "Stride.Core.Assets.Editor",
        "Stride.Core.Assets.Quantum",
        "Stride.Core.BuildEngine.Common",
        "Stride.Core.Design",
        "Stride.Core.IO",
        "Stride.Core.Mathematics",
        "Stride.Core.MicroThreading",
        "Stride.Core.Packages",
        "Stride.Core.Presentation",
        "Stride.Core.Presentation.Dialogs",
        "Stride.Core.Presentation.Graph",
        "Stride.Core.Presentation.Quantum",
        "Stride.Core.ProjectTemplating",
        "Stride.Core.Quantum",
        "Stride.Core.Reflection",
        "Stride.Core.Serialization",
        "Stride.Core.Shaders",
        "Stride.Core.Tasks",
        "Stride.Core.Translation",
        "Stride.Core.Translation.Extractor",
        "Stride.Core.Yaml",
        "Stride.Debugger",
        "Stride.Editor",
        "Stride.EffectCompilerServer",
        "Stride.Graphics",
        "Stride.Graphics.RenderDocPlugin",
        "Stride.Input",
        "Stride.Rendering",
        "Stride.Samples.Templates",
        "Stride.Shaders.Compiler",
        "Stride.Assets",
        "Stride.Audio",
        "Stride.Native",
        "Stride.VirtualReality",
        "Stride.GameStudio",
        "Stride.Games",
        "Stride.Games.Testing",
        "Stride.Importer.Assimp",
        "Stride.Importer.Common",
        "Stride.Importer.Fbx",
        "Stride.Irony",
        "Stride.SamplesTestServer",
        "Stride.Shaders",
        "Stride.Shaders.Parser",
        "Stride.SpriteStudio.Offline",
        "Stride.SpriteStudio.Runtime",
        "Stride.TextureConverter",
        "Stride.VisualStudio.Commands",
        "Stride.VisualStudio.Commands.Interfaces",
        "Stride.Voxels",
    ];
}

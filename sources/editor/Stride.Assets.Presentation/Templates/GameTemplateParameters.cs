// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Templates;
using Xenko.Graphics;

namespace Xenko.Assets.Presentation.Templates
{
    public class XenkoTemplateParameters
    {
        public XenkoTemplateParameters()
        {
            Options = new Dictionary<string, object>();
        }

        public TemplateGeneratorParameters Common { get; set; }

        public Dictionary<string, object> Options { get; private set; }
    }


    public class GameTemplateParameters : XenkoTemplateParameters
    {
        public GameTemplateParameters()
        {
            GraphicsProfile = GraphicsProfile.Level_10_0;
        }

        public List<SolutionPlatform> Platforms { get; set; }

        public bool IsHDR { get; set; }

        public GraphicsProfile GraphicsProfile { get; set; }

        public bool ForcePlatformRegeneration { get; set; }

        public DisplayOrientation Orientation { get; set; }
    }
}

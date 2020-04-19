// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Stride.Core.Storage;
using Stride.Graphics;
using Stride.Core.Shaders.Parser;

namespace Stride.Shaders.Parser
{
    public class ShaderMixinParsingResult : ParsingResult
    {
        public ShaderMixinParsingResult()
        {
            EntryPoints = new Dictionary<ShaderStage, string>();
            HashSources = new HashSourceCollection();
        }

        public EffectReflection Reflection { get; set; }

        public Dictionary<ShaderStage, string> EntryPoints;

        public HashSourceCollection HashSources { get; set; }
    }
}

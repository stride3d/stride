// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Xenko.Rendering
{
    public struct ActiveRenderStage
    {
        public bool Active => EffectSelector != null;

        public EffectSelector EffectSelector;

        public ActiveRenderStage(string effectName)
        {
            EffectSelector = new EffectSelector(effectName);
        }
    }
}

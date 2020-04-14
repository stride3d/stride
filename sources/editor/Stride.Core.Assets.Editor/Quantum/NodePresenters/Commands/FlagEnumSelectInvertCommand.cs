// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Extensions;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands
{
    public class FlagEnumSelectInvertCommand : FlagEnumSelectionCommandBase
    {
        /// <summary>
        /// The name of this command.
        /// </summary>
        public const string CommandName = "FlagEnumSelectInvert";

        /// inheritdoc/>
        public override string Name => CommandName;

        /// inheritdoc/>
        protected override Enum UpdateSelection(Enum currentValue)
        {
            var allFlags = EnumExtensions.GetIndividualFlags(currentValue.GetType());
            var currentFlags = currentValue.GetIndividualFlags();
            var invertFlags = allFlags.Where(x => !currentFlags.Contains(x));
            return EnumExtensions.GetEnum(currentValue.GetType(), invertFlags);
        }
    }
}

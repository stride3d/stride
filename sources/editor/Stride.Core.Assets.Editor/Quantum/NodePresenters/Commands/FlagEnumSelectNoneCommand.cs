// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Extensions;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands
{
    public class FlagEnumSelectNoneCommand : FlagEnumSelectionCommandBase
    {
        /// <summary>
        /// The name of this command.
        /// </summary>
        public const string CommandName = "FlagEnumSelectNone";

        /// inheritdoc/>
        public override string Name => CommandName;

        /// inheritdoc/>
        protected override Enum UpdateSelection(Enum currentValue)
        {
            return EnumExtensions.GetEnum(currentValue.GetType(), Enumerable.Empty<Enum>());
        }
    }
}

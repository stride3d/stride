// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Extensions;
using Xenko.Core.Presentation.Extensions;

namespace Xenko.Core.Assets.Editor.Quantum.NodePresenters.Commands
{
    public class FlagEnumSelectAllCommand : FlagEnumSelectionCommandBase
    {
        /// <summary>
        /// The name of this command.
        /// </summary>
        public const string CommandName = "FlagEnumSelectAll";

        /// inheritdoc/>
        public override string Name => CommandName;

        /// inheritdoc/>
        protected override Enum UpdateSelection(Enum currentValue)
        {
            return EnumExtensions.GetEnum(currentValue.GetType(), EnumExtensions.GetIndividualFlags(currentValue.GetType()));
        }
    }
}

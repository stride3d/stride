// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Reflection;
using Xenko.Core.Annotations;
using Xenko.Core.Presentation.Quantum.Presenters;

namespace Xenko.Core.Assets.Editor.Quantum.NodePresenters.Commands
{
    public abstract class FlagEnumSelectionCommandBase : ChangeValueCommandBase
    {
        /// inheritdoc/>
        public override bool CanAttach(INodePresenter nodePresenter)
        {
            return nodePresenter.Type.IsEnum && nodePresenter.Type.GetCustomAttribute<FlagsAttribute>() != null;
        }

        /// inheritdoc/>
        protected override object ChangeValue(object currentValue, object parameter, object preExecuteResult)
        {
            var result = UpdateSelection((Enum)currentValue);
            return result;
        }

        /// <summary>
        /// Updates the selected flags.
        /// </summary>
        /// <param name="currentValue">The current flags.</param>
        /// <returns>The new flags.</returns>
        [NotNull]
        protected abstract Enum UpdateSelection([NotNull] Enum currentValue);
    }
}

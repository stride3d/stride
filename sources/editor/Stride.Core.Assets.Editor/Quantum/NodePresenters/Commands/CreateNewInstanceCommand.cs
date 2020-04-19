// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Reflection;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands
{
    public class CreateNewInstanceCommand : ChangeValueCommandBase
    {
        /// <summary>
        /// The name of this command.
        /// </summary>
        public const string CommandName = "CreateNewInstance";

        /// <inheritdoc/>
        public override string Name => CommandName;

        /// <inheritdoc/>
        public override CombineMode CombineMode => CombineMode.CombineOnlyForAll;

        /// <inheritdoc/>
        public override bool CanAttach(INodePresenter nodePresenter)
        {
            // We are not in a collection...
            var collectionDescriptor = nodePresenter.Descriptor as CollectionDescriptor;
            if (collectionDescriptor != null)
                return false;

            var type = nodePresenter.Type;
            var isNullableStruct = Nullable.GetUnderlyingType(type)?.IsStruct() ?? false;
            var isAbstractOrClass = type.IsAbstract || type.IsClass;
            var result = isNullableStruct || isAbstractOrClass;
            return result;
        }

        /// <inheritdoc/>
        protected override object ChangeValue(object currentValue, object parameter, object preExecuteResult)
        {
            var entry = (AbstractNodeEntry)parameter;

            // If value is already OK, keep it
            if (entry.IsMatchingValue(currentValue))
                return currentValue;

            return entry.GenerateValue(currentValue);
        }
    }
}

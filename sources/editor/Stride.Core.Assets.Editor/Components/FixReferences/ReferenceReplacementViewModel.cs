// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModel;

namespace Stride.Core.Assets.Editor.Components.FixReferences
{
    public abstract class ReferenceReplacementViewModel<T> : DispatcherViewModel where T : ViewModelBase
    {
        private readonly FixReferencesViewModel<T> fixReferences;
        private readonly object referencedMember;
        private T replacementObject;
        private bool dontFixReference;

        protected ReferenceReplacementViewModel(FixReferencesViewModel<T> fixReferences, T objectToFix, T referencer, object referencedMember)
            : base(fixReferences.SafeArgument(nameof(fixReferences)).ServiceProvider)
        {
            this.fixReferences = fixReferences;
            this.referencedMember = referencedMember;
            Referencer = referencer;
            ObjectToFix = objectToFix;
            PickupReplacementObjectCommand = new AnonymousTaskCommand(fixReferences.ServiceProvider, PickupReplacementObject);
        }

        public T ObjectToFix { get; }

        public T Referencer { get; }

        public T ReplacementObject { get { return replacementObject; } set { SetValue(ref replacementObject, value, () => fixReferences.UpdateCommands()); } }

        public bool DontFixReference { get { return dontFixReference; } set { SetValue(ref dontFixReference, value, () => fixReferences.UpdateCommands()); } }

        public ICommandBase PickupReplacementObjectCommand { get; }

        public void FixReference()
        {
            if (DontFixReference)
            {
                ClearReference();
            }
            else
            {
                if (ReplacementObject == null)
                    throw new InvalidOperationException("The replacement asset can't be null when fixing a reference");

                ReplaceReference();
            }
        }

        public abstract void ClearReference();

        protected abstract void ReplaceReference();

        protected async Task PickupReplacementObject()
        {
            var replacement = await fixReferences.PickupObject(ObjectToFix, referencedMember);
            if (replacement != null)
            {
                ReplacementObject = replacement;
            }
        }
    }
}

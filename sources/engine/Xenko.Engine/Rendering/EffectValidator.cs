// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Xenko.Core.Collections;
using Xenko.Core.Extensions;
using Xenko.Core.IL;
using Xenko.Rendering;

namespace Xenko.Rendering
{
    /// <summary>
    /// How to use:
    /// BeginEffectValidation();
    /// ValidateParameter(key1, value1);
    /// ValidateParameter(key2, value2);
    /// ...
    /// EndEffectValidation(); //returns true if same as last time, false if something changed
    /// You can use EffectValues to actually compile the effect.
    /// </summary>
    public struct EffectValidator
    {
        internal FastListStruct<EffectParameterEntry> EffectValues;
        private int effectValuesValidated; // This is used when validating
        private bool effectChanged;

        /// <summary>
        /// Sets this property to <c>true</c> to skip this effect.
        /// </summary>
        public bool ShouldSkip { get; set; }

        public void Initialize()
        {
            EffectValues = new FastListStruct<EffectParameterEntry>(4);
            
            // Add a dummy value so that an effect without parameter fails validation first time
            EffectValues.Add(new EffectParameterEntry());
        }

        public void BeginEffectValidation()
        {
            effectValuesValidated = 0;
            effectChanged = false;
            ShouldSkip = false;
        }

        [RemoveInitLocals]
        public void ValidateParameter<T>(PermutationParameterKey<T> key, T value)
        {
            // Check if value was existing and/or same
            var index = effectValuesValidated++;
            if (index < EffectValues.Count)
            {
                var currentEffectValue = EffectValues.Items[index];
                if (currentEffectValue.Key == key && EqualityComparer<T>.Default.Equals((T)currentEffectValue.Value, value))
                {
                    // Everything same, let's keep going
                    return;
                }

                // Something was different, let's replace item and clear end of list
                EffectValues[index] = new EffectParameterEntry(key, value);
                EffectValues.Count = effectValuesValidated;
                effectChanged = true;
            }
            else
            {
                EffectValues.Add(new EffectParameterEntry(key, value));
                effectChanged = true;
            }
        }
        
        public bool EndEffectValidation()
        {
            if (effectValuesValidated < EffectValues.Count)
            {
                // Erase extra values
                EffectValues.Count = effectValuesValidated;
                return false;
            }

            return !effectChanged && effectValuesValidated == EffectValues.Count;
        }

        internal struct EffectParameterEntry
        {
            public readonly ParameterKey Key;
            public readonly object Value;

            public EffectParameterEntry(ParameterKey key, object value)
            {
                Key = key;
                Value = value;
            }

            public override string ToString()
            {
                return $"[{Key}, {Value}]";
            }
        }
    }
}

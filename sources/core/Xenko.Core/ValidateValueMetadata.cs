// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1402 // File may only contain a single class
using System;
using Xenko.Core.Annotations;

namespace Xenko.Core
{
    /// <summary>
    /// Delegate ValidateValueCallback used by <see cref="ValidateValueMetadata"/>.
    /// </summary>
    /// <param name="value">The value to validate and coerce.</param>
    public delegate void ValidateValueCallback<T>(ref T value);

    public abstract class ValidateValueMetadata : PropertyKeyMetadata
    {
        [NotNull]
        public static ValidateValueMetadata<T> New<T>([NotNull] ValidateValueCallback<T> invalidationCallback)
        {
            return new ValidateValueMetadata<T>(invalidationCallback);
        }

        public abstract void Validate(ref object obj);
    }

    /// <summary>
    /// A metadata to allow validation/coercision of a value before storing the value into the <see cref="PropertyContainer"/>.
    /// </summary>
    public class ValidateValueMetadata<T> : ValidateValueMetadata
    {
        private readonly ValidateValueCallback<T> validateValueCallback;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidateValueMetadata{T}"/> class.
        /// </summary>
        /// <param name="validateValueCallback">The validate value callback.</param>
        /// <exception cref="System.ArgumentNullException">validateValueCallback</exception>
        public ValidateValueMetadata([NotNull] ValidateValueCallback<T> validateValueCallback)
        {
            if (validateValueCallback == null) throw new ArgumentNullException(nameof(validateValueCallback));
            this.validateValueCallback = validateValueCallback;
        }

        /// <summary>
        /// Gets the validate value callback.
        /// </summary>
        /// <value>The validate value callback.</value>
        public ValidateValueCallback<T> ValidateValueCallback
        {
            get
            {
                return validateValueCallback;
            }
        }

        public override void Validate(ref object obj)
        {
            var objCopy = (T)obj;
            validateValueCallback(ref objCopy);
            obj = objCopy;
        }
    }
}

// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1402 // File may only contain a single class

namespace Stride.Core;

/// <summary>
/// Delegate ValidateValueCallback used by <see cref="ValidateValueMetadata"/>.
/// </summary>
/// <param name="value">The value to validate and coerce.</param>
public delegate void ValidateValueCallback<T>(ref T? value);

public abstract class ValidateValueMetadata : PropertyKeyMetadata
{
    public static ValidateValueMetadata<T> New<T>(ValidateValueCallback<T> invalidationCallback)
    {
        return new ValidateValueMetadata<T>(invalidationCallback);
    }

    public abstract void Validate(ref object? obj);
}

/// <summary>
/// A metadata to allow validation/coercision of a value before storing the value into the <see cref="PropertyContainer"/>.
/// </summary>
public class ValidateValueMetadata<T> : ValidateValueMetadata
{

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidateValueMetadata{T}"/> class.
    /// </summary>
    /// <param name="validateValueCallback">The validate value callback.</param>
    /// <exception cref="ArgumentNullException">validateValueCallback</exception>
    public ValidateValueMetadata(ValidateValueCallback<T> validateValueCallback)
    {
        ArgumentNullException.ThrowIfNull(validateValueCallback);
        this.ValidateValueCallback = validateValueCallback;
    }

    /// <summary>
    /// Gets the validate value callback.
    /// </summary>
    /// <value>The validate value callback.</value>
    public ValidateValueCallback<T> ValidateValueCallback { get; }

    public override void Validate(ref object? obj)
    {
        var objCopy = (T?)obj;
        ValidateValueCallback(ref objCopy);
        obj = objCopy;
    }
}

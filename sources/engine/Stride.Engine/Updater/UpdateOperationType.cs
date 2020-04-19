// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Updater
{
    /// <summary>
    /// Defines the type of <see cref="UpdateOperation"/>.
    /// </summary>
    public enum UpdateOperationType
    {
        /// <summary>
        /// Do not use.
        /// </summary>
        Invalid,

        /// <summary>
        /// Push current state on stack and enter in a reference property. New offset will be beginning of the object.
        /// </summary>
        EnterObjectProperty,

        /// <summary>
        /// Push current state on stack and enter in a value type property.
        /// Object will be copied in a preallocated boxed object so that property setter can be called with updated value when done.
        /// New offset will be beginning of unboxed struct.
        /// Non blittable types are allowed.
        /// </summary>
        EnterStructPropertyBase,

        /// <summary>
        /// Push current state on stack and enter in a reference field. New offset will be beginning of the object.
        /// </summary>
        EnterObjectField,

        /// <summary>
        /// Push current state on stack and enter in a reference property. New offset will be beginning of the object.
        /// </summary>
        EnterObjectCustom,

        /// <summary>
        /// Pop current state.
        /// </summary>
        Leave,

        /// <summary>
        /// Pop current state and set back property into its parent container.
        /// </summary>
        LeaveAndCopyStructPropertyBase, // Need to copy back

        /// <summary>
        /// Set a reference property.
        /// Offset should be the beginning of object containing this property.
        /// </summary>
        ConditionalSetObjectProperty,

        /// <summary>
        /// Set a blittable struct property.
        /// Offset should be the beginning of object containing this property.
        /// </summary>
        ConditionalSetBlittablePropertyBase,

        /// <summary>
        /// Set a struct property.
        /// Offset should be the beginning of object containing this property.
        /// </summary>
        ConditionalSetStructPropertyBase,

        /// <summary>
        /// Set an object field using AnimOperation.Object at the current offset.
        /// </summary>
        ConditionalSetObjectField,

        /// <summary>
        /// Set a blittable struct field using AnimOperation.Data at the current offset.
        /// </summary>
        ConditionalSetBlittableField,

        /// <summary>
        /// Set a blittable struct field using AnimOperation.Data at the current offset. Optimized version for 4 bytes struct.
        /// </summary>
        ConditionalSetBlittableField4,

        /// <summary>
        /// Set a blittable struct field using AnimOperation.Data at the current offset. Optimized version for 8 bytes struct.
        /// </summary>
        ConditionalSetBlittableField8,

        /// <summary>
        /// Set a blittable struct field using AnimOperation.Data at the current offset. Optimized version for 12 bytes struct.
        /// </summary>
        ConditionalSetBlittableField12,

        /// <summary>
        /// Set a blittable struct field using AnimOperation.Data at the current offset. Optimized version for 16 bytes struct.
        /// </summary>
        ConditionalSetBlittableField16,

        /// <summary>
        /// Set a struct field using AnimOperation.Object (boxed) at the current offset.
        /// </summary>
        ConditionalSetStructField,

        /// <summary>
        /// Set an object using AnimOperation.Object at the current offset.
        /// </summary>
        ConditionalSetObjectCustom,
    }
}

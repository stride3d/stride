// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace FreeImageAPI;

//
// Summary:
//     Encapsulates a metadata property to be included in an image file. Not inheritable.
public sealed class PropertyItem
{
    //
    // Summary:
    //     Gets or sets the ID of the property.
    //
    // Returns:
    //     The integer that represents the ID of the property.
    public int Id { get; set; }
    //
    // Summary:
    //     Gets or sets the length (in bytes) of the System.Drawing.Imaging.PropertyItem.Value
    //     property.
    //
    // Returns:
    //     An integer that represents the length (in bytes) of the System.Drawing.Imaging.PropertyItem.Value
    //     byte array.
    public int Len { get; set; }
    //
    // Summary:
    //     Gets or sets an integer that defines the type of data contained in the System.Drawing.Imaging.PropertyItem.Value
    //     property.
    //
    // Returns:
    //     An integer that defines the type of data contained in System.Drawing.Imaging.PropertyItem.Value.
    public short Type { get; set; }
    //
    // Summary:
    //     Gets or sets the value of the property item.
    //
    // Returns:
    //     A byte array that represents the value of the property item.
    public byte[]? Value { get; set; }
}

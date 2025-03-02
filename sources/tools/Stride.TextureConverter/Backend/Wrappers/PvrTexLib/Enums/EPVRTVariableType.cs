// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.TextureConverter.PvrttWrapper;

internal enum EPVRTVariableType
{
    UnsignedByteNorm,
    SignedByteNorm,
    UnsignedByte,
    SignedByte,
    UnsignedShortNorm,
    SignedShortNorm,
    UnsignedShort,
    SignedShort,
    UnsignedIntegerNorm,
    SignedIntegerNorm,
    UnsignedInteger,
    SignedInteger,
    SignedFloat, Float = SignedFloat, //the name ePVRTVarTypeFloat is now deprecated.
    UnsignedFloat,
    NumVarTypes,
	Invalid = 255
}
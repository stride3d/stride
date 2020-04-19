// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#include "stdafx.h"
#include <assert.h>

// Conversion functions
Quaternion AxisRotationToQuaternion(Vector3 axisRotation)
{
	return Quaternion::RotationX(axisRotation.X) * Quaternion::RotationY(axisRotation.Y) * Quaternion::RotationZ(axisRotation.Z);
}
Quaternion AxisRotationToQuaternion(FbxDouble3 axisRotation)
{
	return AxisRotationToQuaternion(FbxDouble3ToVector3(axisRotation));
}

Vector2 FbxDouble2ToVector2(FbxDouble2 vector)
{
	return Vector2((float)vector[0], (float)vector[1]);
}

Color4 FbxDouble3ToColor4(FbxDouble3 vector, float alphaValue)
{
	return Color4((float)vector[0], (float)vector[1], (float)vector[2], alphaValue);
}

Vector3 FbxDouble3ToVector3(FbxDouble3 vector)
{
	return Vector3((float)vector[0], (float)vector[1], (float)vector[2]);
}

Vector4 FbxDouble3ToVector4(FbxDouble3 vector, float wValue)
{
	return Vector4((float)vector[0], (float)vector[1], (float)vector[2], wValue);
}

Vector4 FbxDouble4ToVector4(FbxDouble4 vector)
{
	return Vector4((float)vector[0], (float)vector[1], (float)vector[2], (float)vector[3]);
}

CompressedTimeSpan FBXTimeToTimeSpan(const FbxTime& time)
{
	double resultTime = (double)time.Get();
	resultTime *= (double)CompressedTimeSpan::TicksPerSecond / (double)FBXSDK_TIME_ONE_SECOND.Get();
	return CompressedTimeSpan((int)resultTime);
}

Matrix FBXMatrixToMatrix(FbxAMatrix& matrix)
{
	Matrix result;

	for (int i = 0; i < 4; ++i)
		for (int j = 0; j < 4; ++j)
			((float*)&result)[i * 4 + j] = (float)((double*)&matrix)[j * 4 + i];

	return result;
}

FbxAMatrix MatrixToFBXMatrix(Matrix& matrix)
{
	FbxAMatrix result;

	for (int i = 0; i < 4; ++i)
		for (int j = 0; j < 4; ++j)
			((double*)&result)[i * 4 + j] = (double)((float*)&matrix)[j * 4 + i];

	return result;
}

double FocalLengthToVerticalFov(double filmHeight, double focalLength)
{
	return 2.0 * Math::Atan(filmHeight * 0.5 * 10.0 * 2.54 / focalLength);
}

// Operators
FbxDouble3 operator*(double factor, FbxDouble3 vector)
{
	return FbxDouble3(factor * vector[0], factor * vector[1], factor * vector[2]);
}

// string manipulation
System::String^ ConvertToUTF8(std::string str)
{
	auto byteCount = str.length();
	// Check `str' cannot be more than the size of a int.
	assert(byteCount <= INT32_MAX);
	if (byteCount <= 0)
	{
		return "";
	}
	array<Byte>^ bytes = gcnew array<Byte>((int) byteCount);
	pin_ptr<Byte> p = &bytes[0];
	memcpy(p, str.c_str(), byteCount);
	return System::Text::Encoding::UTF8->GetString(bytes);
}

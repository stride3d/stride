// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma once
#include "stdafx.h"

using namespace System;
using namespace Stride::Core::Mathematics;
using namespace Stride::Animations;

// conversion functions
Color4 FbxDouble3ToColor4(FbxDouble3 vector, float alphaValue = 1.0f);

Vector3 FbxDouble3ToVector3(FbxDouble3 vector);
Vector4 FbxDouble3ToVector4(FbxDouble3 vector, float wValue = 0.0f);

Vector4 FbxDouble4ToVector4(FbxDouble4 vector);

Matrix FBXMatrixToMatrix(FbxAMatrix& matrix);
FbxAMatrix MatrixToFBXMatrix(Matrix& matrix);

Quaternion AxisRotationToQuaternion(Vector3 axisRotation);
Quaternion AxisRotationToQuaternion(FbxDouble3 axisRotation);

CompressedTimeSpan FBXTimeToTimeSpan(const FbxTime& time);

double FocalLengthToVerticalFov(double filmHeight, double focalLength);

// operators
FbxDouble3 operator*(double factor, FbxDouble3 vector);

// string manipulation
System::String^ ConvertToUTF8(std::string str);

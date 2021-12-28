// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#include "Stdafx.h"

#include "UtilityFunctions.h"

using namespace System::Text;
using namespace Stride::Engine;


String^ aiStringToString(aiString str)
{
	// Assimp aiString underlying encoding is UTF-8
	// Windows String underlying encoding is UTF-16
	Encoding^ srcEncoding = Encoding::UTF8;

	const char* pAiData = str.C_Str(); // pointer to the underlying data of he aiString
        // Check `str' cannot be more than the size of a int.
	array<unsigned char>^ buffer = gcnew array<unsigned char>((int) str.length);
	for(unsigned int i=0; i<str.length; ++i)
		buffer[i] = pAiData[i];

	return srcEncoding->GetString(buffer);
}

Color aiColor4ToColor(aiColor4D color)
{
	Color ret(color.r, color.g, color.b, color.a);
	return ret;
}

Color3 aiColor3ToColor3(aiColor3D color)
{
	Color3 ret(color.r, color.g, color.b);
	return ret;
}

Color4 aiColor3ToColor4(aiColor3D color)
{
	Color4 ret(color.r, color.g, color.b, 1.0f);
	return ret;
}

Matrix aiMatrixToMatrix(aiMatrix4x4 mat)
{
	Matrix ret(	mat.a1, mat.b1, mat.c1, mat.d1, 
				mat.a2, mat.b2, mat.c2, mat.d2,
				mat.a3, mat.b3, mat.c3, mat.d3,
				mat.a4, mat.b4, mat.c4, mat.d4);
	return ret;
}

Vector2 aiVector2ToVector2(aiVector2D vec)
{
	Vector2 ret(vec.x, vec.y);
	return ret;
}

Vector3 aiVector3ToVector3(aiVector3D vec)
{
	Vector3 ret(vec.x, vec.y, vec.z);
	return ret;
}

Quaternion aiQuaternionToQuaternion(aiQuaterniont<float> quat)
{
	Quaternion ret(quat.x, quat.y, quat.z, quat.w);
	return ret;
}

CompressedTimeSpan aiTimeToXkTimeSpan(double time, double aiTickPerSecond)
{
	double sdTime = CompressedTimeSpan::TicksPerSecond / aiTickPerSecond * time;
	return CompressedTimeSpan((long)sdTime);
}


Vector3 QuaternionToEulerAngles(Quaternion q)
{
	Vector3 ret;

	// source wikipedia => http://en.wikipedia.org/wiki/Conversion_between_quaternions_and_Euler_angles#Conversion
	ret.X = (float)Math::Atan2(2.f * q.X * q.W + 2.f * q.Y * q.Z, 1 - 2.f * (q.X*q.X  + q.Y*q.Y)); 

	auto sinY = Math::Min(1.0, Math::Max(-1.0, 2.0 * ( q.W * q.Y - q.Z * q.X )));	// with approximation value can exceed 1 and lead to invalid Arcsinus.
	ret.Y = (float)Math::Asin(sinY);          

	ret.Z = (float)Math::Atan2(2.f * q.W * q.Z + 2.f * q.Y * q.X, 1 - 2.f * (q.Y*q.Y + q.Z*q.Z));      

	return ret;
}


Vector3 FlipYZAxis(Vector3 input, bool shouldFlip)
{
	if(!shouldFlip)
		return input;
	
	return Vector3(input.X, input.Z, -input.Y);
}

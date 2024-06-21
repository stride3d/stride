// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.TextureConverter.PvrttWrapper;

internal enum ECompressorQuality
{
    PVRTCFastest = 0,
	PVRTCFast,			
	PVRTCLow,
	PVRTCNormal,
	PVRTCHigh,
	PVRTCVeryHigh,
	PVRTCThorough,
	PVRTCBest,
	NumPVRTCModes,

	ETCFast = 0,
	ETCNormal,
	ETCSlow,
	NumETCModes,

	ASTCVeryFast = 0,
	ASTCFast,
	ASTCMedium,
	ASTCThorough,
	ASTCExhaustive,
	NumASTCModes,
    
	BASISULowest = 0,
	BASISULow,
	BASISUNormal,
	BASISUHigh,
	BASISUBest,
	NumBASISUModes,
}

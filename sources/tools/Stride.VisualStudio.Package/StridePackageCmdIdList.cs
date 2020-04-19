// PkgCmdID.cs
// MUST match PkgCmdID.h
using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;

namespace Stride.VisualStudio
{
    static class StridePackageCmdIdList
    {
        public const uint cmdStridePlatformSelect =        0x100;
        public const uint cmdStrideOpenWithGameStudio = 0x101;
        public const uint cmdStridePlatformSelectList = 0x102;
        public const uint cmdStrideCleanIntermediateAssetsSolutionCommand = 0x103;
        public const uint cmdStrideCleanIntermediateAssetsProjectCommand = 0x104;
    };
}
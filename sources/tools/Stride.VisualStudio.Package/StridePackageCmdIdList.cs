// PkgCmdID.cs
// MUST match PkgCmdID.h
using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;

namespace Xenko.VisualStudio
{
    static class XenkoPackageCmdIdList
    {
        public const uint cmdXenkoPlatformSelect =        0x100;
        public const uint cmdXenkoOpenWithGameStudio = 0x101;
        public const uint cmdXenkoPlatformSelectList = 0x102;
        public const uint cmdXenkoCleanIntermediateAssetsSolutionCommand = 0x103;
        public const uint cmdXenkoCleanIntermediateAssetsProjectCommand = 0x104;
    };
}
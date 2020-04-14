// Guids.cs
// MUST match guids.h
using System;

namespace Xenko.VisualStudio
{
    internal static class GuidList
    {
        public const string guidXenko_VisualStudio_PackagePkgString = "b0b8feb1-7b83-43fc-9fc0-70065ddb80a1";
        public const string guidXenko_VisualStudio_PackageCmdSetString = "9428db93-bfea-4115-8d4a-40b047166e61";
        public const string guidToolWindowPersistanceString = "ddd10155-9f63-4694-95ce-c7ba2d74ad46";

        public const string guidXenko_VisualStudio_ShaderKeyFileGenerator = "b50e6ece-b11f-477b-a8e1-1e60e0531a53";
        public const string guidXenko_VisualStudio_EffectCodeFileGenerator = "e6259cfb-c775-426e-b499-f57d0a3ba2c1";

        public const string guidXenko_VisualStudio_DataCodeGenerator = "22555301-d58a-4d71-9dab-b2552cc3de0e";

        public static readonly Guid guidXenko_VisualStudio_PackageCmdSet = new Guid(guidXenko_VisualStudio_PackageCmdSetString);

        public const string vsContextGuidVCSProject = "{fae04ec1-301f-11d3-bf4b-00c04f79efbc}";
        public const string vsContextGuidVCSNewProject = "{9A19103F-16F7-4668-BE54-9A1E7A4F7556}";
    };
}

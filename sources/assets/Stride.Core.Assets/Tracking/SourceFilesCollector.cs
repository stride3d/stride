// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Visitors;
using Stride.Core.IO;
using Stride.Core.Reflection;

namespace Stride.Core.Assets.Tracking;

public class SourceFilesCollector : AssetVisitorBase
{
    private Dictionary<UFile, bool>? sourceFiles;
    private HashSet<UFile>? compilationInputFiles;
    private Dictionary<MemberPath, UFile>? sourceMembers;

    public Dictionary<UFile, bool> GetSourceFiles(Asset asset)
    {
        sourceFiles = [];
        Visit(asset);
        var result = sourceFiles;
        sourceFiles = null;
        return result;
    }

    public HashSet<UFile> GetCompilationInputFiles(Asset asset)
    {
        compilationInputFiles = [];
        Visit(asset);
        var result = compilationInputFiles;
        compilationInputFiles = null;
        return result;
    }

    public Dictionary<MemberPath, UFile> GetSourceMembers(Asset asset)
    {
        sourceMembers = [];
        Visit(asset);
        var result = sourceMembers;
        sourceMembers = null;
        return result;
    }

    public override void VisitObjectMember(object container, ObjectDescriptor containerDescriptor, IMemberDescriptor member, object? value)
    {
        if (sourceFiles is not null)
        {
            if (member.Type == typeof(UFile) && value is not null)
            {
                var file = (UFile)value;
                if (!string.IsNullOrWhiteSpace(file.ToString()))
                {
                    var attribute = member.GetCustomAttributes<SourceFileMemberAttribute>(true).SingleOrDefault();
                    if (attribute is not null)
                    {
                        if (!sourceFiles.ContainsKey(file))
                        {
                            sourceFiles.Add(file, attribute.UpdateAssetIfChanged);
                        }
                        else if (attribute.UpdateAssetIfChanged)
                        {
                            // If the file has already been collected, just update whether it should update the asset when changed
                            sourceFiles[file] = true;
                        }
                    }
                }
            }
        }
        if (compilationInputFiles is not null)
        {
            if (member.Type == typeof(UFile) && value is not null)
            {
                var file = (UFile)value;
                if (!string.IsNullOrWhiteSpace(file.ToString()))
                {
                    var attribute = member.GetCustomAttributes<SourceFileMemberAttribute>(true).SingleOrDefault();
                    if (attribute is not null && !attribute.Optional)
                    {
                        compilationInputFiles.Add(file);
                    }
                }
            }
        }
        if (sourceMembers is not null)
        {
            if (member.Type == typeof(UFile))
            {
                var attribute = member.GetCustomAttributes<SourceFileMemberAttribute>(true).SingleOrDefault();
                if (attribute is not null)
                {
                    sourceMembers[CurrentPath.Clone()] = value as UFile;
                }
            }
        }
        base.VisitObjectMember(container, containerDescriptor, member, value);
    }
}

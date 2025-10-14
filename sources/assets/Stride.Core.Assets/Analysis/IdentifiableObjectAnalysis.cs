// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text;
using Stride.Core.Assets.Visitors;
using Stride.Core.Diagnostics;
using Stride.Core.Reflection;

namespace Stride.Core.Assets.Analysis;

/// <summary>
/// A static class that visit an object and make sure that none of the <see cref="IIdentifiable"/> it references share the same identifier. In case there are duplicate identifier,
/// the visitor can generate new identifiers for the duplicate
/// </summary>
public static class IdentifiableObjectAnalysis
{
    /// <summary>
    /// Visits the object and look up for duplicates identifier in <see cref="IIdentifiable"/> instances.
    /// </summary>
    /// <param name="obj">The object to visit.</param>
    /// <param name="fixDuplicate">If true, duplicate identifiers will be fixed by generating new identifiers.</param>
    /// <param name="logger">A logger to report duplicates and fixes.</param>
    /// <returns>True if the given object has been modified, false otherwise.</returns>
    public static bool Visit(object obj, bool fixDuplicate, ILogger? logger = null)
    {
        var visitor = new IdentifiableObjectAnalysisVisitor();
        visitor.Visit(obj);
        var sb = new StringBuilder();
        var hasBeenModified = false;
        foreach (var result in visitor.IdentifiablesById)
        {
            if (result.Value.Count > 1)
            {
                var first = true;

                if (logger != null)
                {
                    sb.Clear();
                    sb.Append($"Multiple object with same id [{result.Key}]");
                }

                foreach (var identifiable in result.Value)
                {
                    if (!first && fixDuplicate)
                    {
                        identifiable.Id = Guid.NewGuid();
                        hasBeenModified = true;
                    }

                    if (logger != null)
                    {
                        sb.Append($"\r\n - One instance of [{identifiable.GetType()}] reachable from the following paths:");
                        if (identifiable.Id != result.Key)
                            sb.Append($" (replaced by new id [{identifiable.Id}]");
                        foreach (var path in visitor.IdentifiablePaths[identifiable])
                        {
                            sb.Append($"\r\n   - [{path}]");
                        }
                    }
                    first = false;
                }
                logger.Warning(sb.ToString());
            }
        }
        return hasBeenModified;
    }

    private class IdentifiableObjectAnalysisVisitor : AssetVisitorBase
    {
        public readonly Dictionary<Guid, HashSet<IIdentifiable>> IdentifiablesById = [];
        public readonly Dictionary<IIdentifiable, List<MemberPath>> IdentifiablePaths = [];

        public override void VisitObject(object obj, ObjectDescriptor descriptor, bool visitMembers)
        {
            if (obj is IIdentifiable identifiable)
            {
                if (!IdentifiablesById.TryGetValue(identifiable.Id, out var identifiables))
                {
                    IdentifiablesById.Add(identifiable.Id, identifiables = []);
                }
                identifiables.Add(identifiable);
                if (!IdentifiablePaths.TryGetValue(identifiable, out var paths))
                {
                    IdentifiablePaths.Add(identifiable, paths = []);
                }
                paths.Add(CurrentPath.Clone());
            }
            base.VisitObject(obj, descriptor, visitMembers);
        }
    }
}

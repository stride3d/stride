// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.CodeAnalysis;
using Mono.Cecil;
using QuickGraph;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Stride.GameStudio.Debugging
{
    partial class AssemblyRecompiler
    {
        /// <summary>
        /// Defines a group of source <see cref="SyntaxTree"/>, and later a generated <see cref="Project"/> and compiled assemblies.
        /// </summary>
        public class SourceGroup : AdjacencyGraph<SyntaxTree, SEdge<SyntaxTree>>
        {
            /// <summary>
            /// Gets or sets the generated roslyn project.
            /// </summary>
            /// <value>
            /// The generated roslyn project.
            /// </value>
            public Project Project { get; set; }

            /// <summary>
            /// Gets or sets the assembly PE data.
            /// </summary>
            /// <value>
            /// The assembly PE data.
            /// </value>
            public byte[] PE { get; set; }

            /// <summary>
            /// Gets or sets the assembly PDB data.
            /// </summary>
            /// <value>
            /// The assembly PDB data.
            /// </value>
            public byte[] PDB { get; set; }

            /// <value>
            /// Temporarily stores the assembly when generating them.
            /// </value>
            internal AssemblyDefinition Assembly { get; set; }

            public override string ToString()
            {
                return string.Join(" ", Vertices.Select(x => Path.GetFileName(x.FilePath)));
            }
        }

        public class SourceGroupComparer : EqualityComparer<SourceGroup>
        {
            private static readonly SourceGroupComparer _default = new SourceGroupComparer();

            /// <summary>
            /// Gets the default.
            /// </summary>
            public new static SourceGroupComparer Default
            {
                get { return _default; }
            }

            public override bool Equals(SourceGroup x, SourceGroup y)
            {
                // Compare if two collection of SyntaxTree are the same
                // Not the best perf-wise, but should be good enough for now
                return new HashSet<SyntaxTree>(x.Vertices).SetEquals(y.Vertices);
            }

            public override int GetHashCode(SourceGroup obj)
            {
                unchecked
                {
                    var hashCode = 17;
                    foreach (var vertex in obj.Vertices.OrderBy(x => x.FilePath))
                    {
                        hashCode = hashCode * 31 + vertex.GetHashCode();
                    }
                    return hashCode;
                }
            }
        }
    }
}

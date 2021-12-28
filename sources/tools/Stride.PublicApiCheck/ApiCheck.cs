// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using Mono.Cecil;

namespace Stride.PublicApiCheck
{
    /// <summary>
    /// Helper class to check public API consistency between assemblies.
    /// </summary>
    public static class ApiCheck
    {

        /// <summary>
        /// Gets all the public API as string items from an assembly.
        /// </summary>
        /// <param name="assemblyRef">The assembly to get the items from.</param>
        /// <returns>An enumeration of string with a fullname API.</returns>
        public static IEnumerable<string> GetPublicApiItems(string assemblyRef)
        {
            var assembly = AssemblyDefinition.ReadAssembly(assemblyRef);
            return assembly.MainModule.Types.SelectMany(GetPublicApiItems);
        }

        /// <summary>
        /// Gets all the public API as string items from a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public static IEnumerable<string> GetPublicApiItems(TypeDefinition type)
        {
            if (!type.IsPublic)
                yield break;

            yield return type.FullName;

            foreach (var field in type.Fields.Where(field => (field.IsPublic || field.IsAssembly)))
            {
                yield return field.FullName;
            }

            foreach (var property in type.Properties)
            {
               if (property.GetMethod != null && (property.GetMethod.IsPublic || property.GetMethod.IsAssembly))
                   yield return property.GetMethod.FullName;

               if (property.SetMethod != null && (property.SetMethod.IsPublic || property.SetMethod.IsAssembly))
                   yield return property.SetMethod.FullName;
            }

            foreach (var method in type.Methods)
            {
                if (method.IsPublic || method.IsAssembly)
                {
                    if (method.IsSetter || method.IsGetter)
                        continue;

                    yield return method.FullName;
                }
            }
        }

        /// <summary>
        /// Performs a diff between the public API of two assemblies.
        /// </summary>
        /// <param name="from">The from assembly (reference).</param>
        /// <param name="to">The to assembly (against).</param>
        /// <returns>A list of public API defined in the [from] assembly but not present in the [to] assembly.</returns>
        public static List<string> DiffAssembly(string from, string to)
        {
            var fromTypes = GetPublicApiItems(from);
            var toTypes = GetPublicApiItems(to);

            return fromTypes.Except(toTypes).ToList();
        }

        /// <summary>
        /// Performs a diff between the public API of two assemblies.
        /// </summary>
        /// <param name="from">The from assembly (reference).</param>
        /// <param name="to">The to assembly (against).</param>
        /// <returns>null if API is the same, elase a string with a list of public API defined in the [from] assembly but not present in the [to] assembly.</returns>
        public static string DiffAssemblyToString(string from, string to)
        {
            var diff = DiffAssembly(from, to);
            if (diff.Count > 0)
            {
                var output = new StringBuilder();
                output.AppendFormat("{0} public missing in {1}", diff.Count, Path.GetFileName(to));
                output.AppendLine();
                foreach (var diffItem in diff)
                {
                    output.AppendLine(diffItem);
                }
                return output.ToString();
            }
            return null;
        }

        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("{0} assemblyRef assemblyAgainst", Path.GetFileName(Assembly.GetEntryAssembly().Location));
                Environment.Exit(-1);
            }

            var diff = DiffAssemblyToString(args[0], args[1]);
            if (diff != null)
            {
                Console.WriteLine(diff);
                Environment.Exit(-1);
            }
        }
    }
}

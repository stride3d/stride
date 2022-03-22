using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Stride.Core.CompilerServices
{
    [DebuggerDisplay("Type={Type}, Members: {Members.Count}")]
    internal class SerializerTypeSpec
    {
        public INamedTypeSymbol Type { get; private set; }

        public bool IsValueType => Type.IsValueType;

        public List<SerializerMemberSpec> Members { get; private set; }

        public SerializerTypeSpec(INamedTypeSymbol type, List<SerializerMemberSpec> members)
        {
            Type = type;
            Members = members;
        }

        public static string GetCompilableName(Type type)
        {
            if (type.IsArray)
            {
                return GetCompilableName(type.GetElementType()) + "[]";
            }

            string compilableName;

            if (!type.IsGenericType)
            {
                compilableName = type.FullName;
            }
            else
            {
                StringBuilder sb = new();

                string fullName = type.FullName;
                int backTickIndex = fullName.IndexOf('`');

                string baseName = fullName.Substring(0, backTickIndex);

                sb.Append(baseName);

                sb.Append("<");

                Type[] genericArgs = type.GetGenericArguments();
                int genericArgCount = genericArgs.Length;
                List<string> genericArgNames = new(genericArgCount);

                for (int i = 0; i < genericArgCount; i++)
                {
                    genericArgNames.Add(GetCompilableName(genericArgs[i]));
                }

                sb.Append(string.Join(", ", genericArgNames));

                sb.Append(">");

                compilableName = sb.ToString();
            }

            compilableName = compilableName.Replace("+", ".");
            //return "global::" + compilableName; // enable this later
            return compilableName;
        }
    }
}

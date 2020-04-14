// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace Stride.Core.AssemblyProcessor
{
    /// <summary>
    /// Visit Cecil types recursively, and replace them if requested.
    /// </summary>
    public class CecilTypeReferenceVisitor
    {
        protected IList<T> VisitDynamicList<T>(IList<T> list) where T : TypeReference
        {
            var result = list;
            for (int i = 0; i < result.Count; i++)
            {
                var item = result[i];

                var newNode = VisitDynamic(item);
                if (newNode == null)
                {
                    if (result == list)
                        result = new List<T>(list);
                    result.RemoveAt(i);
                    i--;
                }
                else if (!ReferenceEquals(newNode, item) && newNode is T)
                {
                    if (result == list)
                        result = new List<T>(list);
                    result[i] = (T)newNode;
                }
            }
            return result;
        }

        public virtual TypeReference VisitDynamic(TypeReference type)
        {
            var arrayType = type as ArrayType;
            if (arrayType != null)
                return Visit(arrayType);

            var genericInstanceType = type as GenericInstanceType;
            if (genericInstanceType != null)
                return Visit(genericInstanceType);

            var genericParameter = type as GenericParameter;
            if (genericParameter != null)
                return Visit(genericParameter);

            var pointerType = type as PointerType;
            if (pointerType != null)
                return Visit(pointerType);

            var pinnedType = type as PinnedType;
            if (pinnedType != null)
                return Visit(pinnedType);

            if (type.GetType() != typeof(TypeReference) && type.GetType() != typeof(TypeDefinition))
                throw new NotSupportedException();

            return Visit(type);
        }

        public virtual TypeReference Visit(GenericParameter type)
        {
            return type;
        }

        public virtual TypeReference Visit(PointerType type)
        {
            type = type.ChangePointerType(VisitDynamic(type.ElementType));
            return type.ChangeGenericParameters(VisitDynamicList(type.GenericParameters));
        }

        public virtual TypeReference Visit(PinnedType type)
        {
            type = type.ChangePinnedType(VisitDynamic(type.ElementType));
            return type.ChangeGenericParameters(VisitDynamicList(type.GenericParameters));
        }

        public virtual TypeReference Visit(TypeReference type)
        {
            return type.ChangeGenericParameters(VisitDynamicList(type.GenericParameters));
        }

        public virtual TypeReference Visit(ArrayType type)
        {
            type = type.ChangeArrayType(VisitDynamic(type.ElementType), type.Rank);
            return type.ChangeGenericParameters(VisitDynamicList(type.GenericParameters));
        }

        public virtual TypeReference Visit(GenericInstanceType type)
        {
            type = type.ChangeGenericInstanceType(VisitDynamic(type.ElementType), VisitDynamicList(type.GenericArguments));
            return type.ChangeGenericParameters(VisitDynamicList(type.GenericParameters));
        }
    }
}

// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

// AUTO-GENERATED, DO NOT EDIT!
#pragma warning disable SA1649 // File name must match first type name
#pragma warning disable SA1402 // File may only contain a single class

namespace Xenko.Rendering
{
    public enum DataType
    {
        ViewObject,
        Object,
        Render,
        EffectObject,
        View,
        StaticObject,
    }

    public struct ViewObjectPropertyData<T>
    {
        internal T[] Data;

        internal ViewObjectPropertyData(T[] data)
        {
            Data = data;
        }

        public ref T this[ViewObjectNodeReference index] => ref Data[index.Index];
    }

    public struct ViewObjectPropertyKey<T>
    {
        internal readonly int Index;

        internal ViewObjectPropertyKey(int index)
        {
            Index = index;
        }
    }

    public class ViewObjectPropertyDefinition<T>
    {
    }

    public partial struct ViewObjectNodeReference
    {
        public readonly int Index;

        /// <summary>
        /// Invalid slot.
        /// </summary>
        public static readonly ViewObjectNodeReference Invalid = new ViewObjectNodeReference(-1);

        public ViewObjectNodeReference(int index)
        {
            Index = index;
        }

        public static ViewObjectNodeReference operator +(ViewObjectNodeReference value, int offset)
        {
            return new ViewObjectNodeReference(value.Index + offset);
        }

        public static ViewObjectNodeReference operator *(ViewObjectNodeReference value, int multiplyFactor)
        {
            return new ViewObjectNodeReference(value.Index * multiplyFactor);
        }

        public static bool operator ==(ViewObjectNodeReference a, ViewObjectNodeReference b)
        {
            return a.Index == b.Index;
        }

        public override bool Equals(object other)
        {
            if (other is ViewObjectNodeReference)
            {
                ViewObjectNodeReference p = (ViewObjectNodeReference)other;
                return Index == p.Index;
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return Index;
        }

        public static bool operator !=(ViewObjectNodeReference a, ViewObjectNodeReference b)
        {
            return a.Index != b.Index;
        }
    }

    partial struct RenderDataHolder
    {
        public ViewObjectPropertyKey<T> CreateViewObjectKey<T>(ViewObjectPropertyDefinition<T> definition = null, int multiplier = 1)
        {
            if (definition != null)
            {
                int existingIndex;
                if (dataArraysByDefinition.TryGetValue(definition, out existingIndex))
                    return new ViewObjectPropertyKey<T>(existingIndex);

                dataArraysByDefinition.Add(definition, dataArrays.Count);
            }

            var dataArraysIndex = dataArrays.Count;
            dataArrays.Add(new DataArray(new DataArrayInfo<T>(DataType.ViewObject, multiplier)));
            return new ViewObjectPropertyKey<T>(dataArraysIndex);
        }

        /// <summary>
        /// Get data from its key.
        /// </summary>
        public ViewObjectPropertyData<T> GetData<T>(ViewObjectPropertyKey<T> key)
        {
            return new ViewObjectPropertyData<T>((T[])dataArrays[key.Index].Array);
        }

        /// <summary>
        /// Change data multiplier (i.e. how many data entries per item there will be).
        /// </summary>
        public void ChangeDataMultiplier<T>(ViewObjectPropertyKey<T> key, int multiplier)
        {
            dataArrays[key.Index].Info.ChangeMutiplier(ref dataArrays.Items[key.Index].Array, multiplier);
        }
    }
    public struct ObjectPropertyData<T>
    {
        internal T[] Data;

        internal ObjectPropertyData(T[] data)
        {
            Data = data;
        }

        public ref T this[ObjectNodeReference index] => ref Data[index.Index];
    }

    public struct ObjectPropertyKey<T>
    {
        internal readonly int Index;

        internal ObjectPropertyKey(int index)
        {
            Index = index;
        }
    }

    public class ObjectPropertyDefinition<T>
    {
    }

    public partial struct ObjectNodeReference
    {
        public readonly int Index;

        /// <summary>
        /// Invalid slot.
        /// </summary>
        public static readonly ObjectNodeReference Invalid = new ObjectNodeReference(-1);

        public ObjectNodeReference(int index)
        {
            Index = index;
        }

        public static ObjectNodeReference operator +(ObjectNodeReference value, int offset)
        {
            return new ObjectNodeReference(value.Index + offset);
        }

        public static ObjectNodeReference operator *(ObjectNodeReference value, int multiplyFactor)
        {
            return new ObjectNodeReference(value.Index * multiplyFactor);
        }

        public static bool operator ==(ObjectNodeReference a, ObjectNodeReference b)
        {
            return a.Index == b.Index;
        }

        public override bool Equals(object other)
        {
            if (other is ObjectNodeReference)
            {
                ObjectNodeReference p = (ObjectNodeReference)other;
                return Index == p.Index;
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return Index;
        }

        public static bool operator !=(ObjectNodeReference a, ObjectNodeReference b)
        {
            return a.Index != b.Index;
        }
    }

    partial struct RenderDataHolder
    {
        public ObjectPropertyKey<T> CreateObjectKey<T>(ObjectPropertyDefinition<T> definition = null, int multiplier = 1)
        {
            if (definition != null)
            {
                int existingIndex;
                if (dataArraysByDefinition.TryGetValue(definition, out existingIndex))
                    return new ObjectPropertyKey<T>(existingIndex);

                dataArraysByDefinition.Add(definition, dataArrays.Count);
            }

            var dataArraysIndex = dataArrays.Count;
            dataArrays.Add(new DataArray(new DataArrayInfo<T>(DataType.Object, multiplier)));
            return new ObjectPropertyKey<T>(dataArraysIndex);
        }

        /// <summary>
        /// Get data from its key.
        /// </summary>
        public ObjectPropertyData<T> GetData<T>(ObjectPropertyKey<T> key)
        {
            return new ObjectPropertyData<T>((T[])dataArrays[key.Index].Array);
        }

        /// <summary>
        /// Change data multiplier (i.e. how many data entries per item there will be).
        /// </summary>
        public void ChangeDataMultiplier<T>(ObjectPropertyKey<T> key, int multiplier)
        {
            dataArrays[key.Index].Info.ChangeMutiplier(ref dataArrays.Items[key.Index].Array, multiplier);
        }
    }
    public struct RenderPropertyData<T>
    {
        internal T[] Data;

        internal RenderPropertyData(T[] data)
        {
            Data = data;
        }

        public ref T this[RenderNodeReference index] => ref Data[index.Index];
    }

    public struct RenderPropertyKey<T>
    {
        internal readonly int Index;

        internal RenderPropertyKey(int index)
        {
            Index = index;
        }
    }

    public class RenderPropertyDefinition<T>
    {
    }

    public partial struct RenderNodeReference
    {
        public readonly int Index;

        /// <summary>
        /// Invalid slot.
        /// </summary>
        public static readonly RenderNodeReference Invalid = new RenderNodeReference(-1);

        public RenderNodeReference(int index)
        {
            Index = index;
        }

        public static RenderNodeReference operator +(RenderNodeReference value, int offset)
        {
            return new RenderNodeReference(value.Index + offset);
        }

        public static RenderNodeReference operator *(RenderNodeReference value, int multiplyFactor)
        {
            return new RenderNodeReference(value.Index * multiplyFactor);
        }

        public static bool operator ==(RenderNodeReference a, RenderNodeReference b)
        {
            return a.Index == b.Index;
        }

        public override bool Equals(object other)
        {
            if (other is RenderNodeReference)
            {
                RenderNodeReference p = (RenderNodeReference)other;
                return Index == p.Index;
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return Index;
        }

        public static bool operator !=(RenderNodeReference a, RenderNodeReference b)
        {
            return a.Index != b.Index;
        }
    }

    partial struct RenderDataHolder
    {
        public RenderPropertyKey<T> CreateRenderKey<T>(RenderPropertyDefinition<T> definition = null, int multiplier = 1)
        {
            if (definition != null)
            {
                int existingIndex;
                if (dataArraysByDefinition.TryGetValue(definition, out existingIndex))
                    return new RenderPropertyKey<T>(existingIndex);

                dataArraysByDefinition.Add(definition, dataArrays.Count);
            }

            var dataArraysIndex = dataArrays.Count;
            dataArrays.Add(new DataArray(new DataArrayInfo<T>(DataType.Render, multiplier)));
            return new RenderPropertyKey<T>(dataArraysIndex);
        }

        /// <summary>
        /// Get data from its key.
        /// </summary>
        public RenderPropertyData<T> GetData<T>(RenderPropertyKey<T> key)
        {
            return new RenderPropertyData<T>((T[])dataArrays[key.Index].Array);
        }

        /// <summary>
        /// Change data multiplier (i.e. how many data entries per item there will be).
        /// </summary>
        public void ChangeDataMultiplier<T>(RenderPropertyKey<T> key, int multiplier)
        {
            dataArrays[key.Index].Info.ChangeMutiplier(ref dataArrays.Items[key.Index].Array, multiplier);
        }
    }
    public struct EffectObjectPropertyData<T>
    {
        internal T[] Data;

        internal EffectObjectPropertyData(T[] data)
        {
            Data = data;
        }

        public ref T this[EffectObjectNodeReference index] => ref Data[index.Index];
    }

    public struct EffectObjectPropertyKey<T>
    {
        internal readonly int Index;

        internal EffectObjectPropertyKey(int index)
        {
            Index = index;
        }
    }

    public class EffectObjectPropertyDefinition<T>
    {
    }

    public partial struct EffectObjectNodeReference
    {
        public readonly int Index;

        /// <summary>
        /// Invalid slot.
        /// </summary>
        public static readonly EffectObjectNodeReference Invalid = new EffectObjectNodeReference(-1);

        public EffectObjectNodeReference(int index)
        {
            Index = index;
        }

        public static EffectObjectNodeReference operator +(EffectObjectNodeReference value, int offset)
        {
            return new EffectObjectNodeReference(value.Index + offset);
        }

        public static EffectObjectNodeReference operator *(EffectObjectNodeReference value, int multiplyFactor)
        {
            return new EffectObjectNodeReference(value.Index * multiplyFactor);
        }

        public static bool operator ==(EffectObjectNodeReference a, EffectObjectNodeReference b)
        {
            return a.Index == b.Index;
        }

        public override bool Equals(object other)
        {
            if (other is EffectObjectNodeReference)
            {
                EffectObjectNodeReference p = (EffectObjectNodeReference)other;
                return Index == p.Index;
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return Index;
        }

        public static bool operator !=(EffectObjectNodeReference a, EffectObjectNodeReference b)
        {
            return a.Index != b.Index;
        }
    }

    partial struct RenderDataHolder
    {
        public EffectObjectPropertyKey<T> CreateEffectObjectKey<T>(EffectObjectPropertyDefinition<T> definition = null, int multiplier = 1)
        {
            if (definition != null)
            {
                int existingIndex;
                if (dataArraysByDefinition.TryGetValue(definition, out existingIndex))
                    return new EffectObjectPropertyKey<T>(existingIndex);

                dataArraysByDefinition.Add(definition, dataArrays.Count);
            }

            var dataArraysIndex = dataArrays.Count;
            dataArrays.Add(new DataArray(new DataArrayInfo<T>(DataType.EffectObject, multiplier)));
            return new EffectObjectPropertyKey<T>(dataArraysIndex);
        }

        /// <summary>
        /// Get data from its key.
        /// </summary>
        public EffectObjectPropertyData<T> GetData<T>(EffectObjectPropertyKey<T> key)
        {
            return new EffectObjectPropertyData<T>((T[])dataArrays[key.Index].Array);
        }

        /// <summary>
        /// Change data multiplier (i.e. how many data entries per item there will be).
        /// </summary>
        public void ChangeDataMultiplier<T>(EffectObjectPropertyKey<T> key, int multiplier)
        {
            dataArrays[key.Index].Info.ChangeMutiplier(ref dataArrays.Items[key.Index].Array, multiplier);
        }
    }
    public struct ViewPropertyData<T>
    {
        internal T[] Data;

        internal ViewPropertyData(T[] data)
        {
            Data = data;
        }

        public ref T this[ViewNodeReference index] => ref Data[index.Index];
    }

    public struct ViewPropertyKey<T>
    {
        internal readonly int Index;

        internal ViewPropertyKey(int index)
        {
            Index = index;
        }
    }

    public class ViewPropertyDefinition<T>
    {
    }

    public partial struct ViewNodeReference
    {
        public readonly int Index;

        /// <summary>
        /// Invalid slot.
        /// </summary>
        public static readonly ViewNodeReference Invalid = new ViewNodeReference(-1);

        public ViewNodeReference(int index)
        {
            Index = index;
        }

        public static ViewNodeReference operator +(ViewNodeReference value, int offset)
        {
            return new ViewNodeReference(value.Index + offset);
        }

        public static ViewNodeReference operator *(ViewNodeReference value, int multiplyFactor)
        {
            return new ViewNodeReference(value.Index * multiplyFactor);
        }

        public static bool operator ==(ViewNodeReference a, ViewNodeReference b)
        {
            return a.Index == b.Index;
        }

        public override bool Equals(object other)
        {
            if (other is ViewNodeReference)
            {
                ViewNodeReference p = (ViewNodeReference)other;
                return Index == p.Index;
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return Index;
        }

        public static bool operator !=(ViewNodeReference a, ViewNodeReference b)
        {
            return a.Index != b.Index;
        }
    }

    partial struct RenderDataHolder
    {
        public ViewPropertyKey<T> CreateViewKey<T>(ViewPropertyDefinition<T> definition = null, int multiplier = 1)
        {
            if (definition != null)
            {
                int existingIndex;
                if (dataArraysByDefinition.TryGetValue(definition, out existingIndex))
                    return new ViewPropertyKey<T>(existingIndex);

                dataArraysByDefinition.Add(definition, dataArrays.Count);
            }

            var dataArraysIndex = dataArrays.Count;
            dataArrays.Add(new DataArray(new DataArrayInfo<T>(DataType.View, multiplier)));
            return new ViewPropertyKey<T>(dataArraysIndex);
        }

        /// <summary>
        /// Get data from its key.
        /// </summary>
        public ViewPropertyData<T> GetData<T>(ViewPropertyKey<T> key)
        {
            return new ViewPropertyData<T>((T[])dataArrays[key.Index].Array);
        }

        /// <summary>
        /// Change data multiplier (i.e. how many data entries per item there will be).
        /// </summary>
        public void ChangeDataMultiplier<T>(ViewPropertyKey<T> key, int multiplier)
        {
            dataArrays[key.Index].Info.ChangeMutiplier(ref dataArrays.Items[key.Index].Array, multiplier);
        }
    }
    public struct StaticObjectPropertyData<T>
    {
        internal T[] Data;

        internal StaticObjectPropertyData(T[] data)
        {
            Data = data;
        }

        public ref T this[StaticObjectNodeReference index] => ref Data[index.Index];
    }

    public struct StaticObjectPropertyKey<T>
    {
        internal readonly int Index;

        internal StaticObjectPropertyKey(int index)
        {
            Index = index;
        }
    }

    public class StaticObjectPropertyDefinition<T>
    {
    }

    public partial struct StaticObjectNodeReference
    {
        public readonly int Index;

        /// <summary>
        /// Invalid slot.
        /// </summary>
        public static readonly StaticObjectNodeReference Invalid = new StaticObjectNodeReference(-1);

        public StaticObjectNodeReference(int index)
        {
            Index = index;
        }

        public static StaticObjectNodeReference operator +(StaticObjectNodeReference value, int offset)
        {
            return new StaticObjectNodeReference(value.Index + offset);
        }

        public static StaticObjectNodeReference operator *(StaticObjectNodeReference value, int multiplyFactor)
        {
            return new StaticObjectNodeReference(value.Index * multiplyFactor);
        }

        public static bool operator ==(StaticObjectNodeReference a, StaticObjectNodeReference b)
        {
            return a.Index == b.Index;
        }

        public override bool Equals(object other)
        {
            if (other is StaticObjectNodeReference)
            {
                StaticObjectNodeReference p = (StaticObjectNodeReference)other;
                return Index == p.Index;
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return Index;
        }

        public static bool operator !=(StaticObjectNodeReference a, StaticObjectNodeReference b)
        {
            return a.Index != b.Index;
        }
    }

    partial struct RenderDataHolder
    {
        public StaticObjectPropertyKey<T> CreateStaticObjectKey<T>(StaticObjectPropertyDefinition<T> definition = null, int multiplier = 1)
        {
            if (definition != null)
            {
                int existingIndex;
                if (dataArraysByDefinition.TryGetValue(definition, out existingIndex))
                    return new StaticObjectPropertyKey<T>(existingIndex);

                dataArraysByDefinition.Add(definition, dataArrays.Count);
            }

            var dataArraysIndex = dataArrays.Count;
            dataArrays.Add(new DataArray(new DataArrayInfo<T>(DataType.StaticObject, multiplier)));
            return new StaticObjectPropertyKey<T>(dataArraysIndex);
        }

        /// <summary>
        /// Get data from its key.
        /// </summary>
        public StaticObjectPropertyData<T> GetData<T>(StaticObjectPropertyKey<T> key)
        {
            return new StaticObjectPropertyData<T>((T[])dataArrays[key.Index].Array);
        }

        /// <summary>
        /// Change data multiplier (i.e. how many data entries per item there will be).
        /// </summary>
        public void ChangeDataMultiplier<T>(StaticObjectPropertyKey<T> key, int multiplier)
        {
            dataArrays[key.Index].Info.ChangeMutiplier(ref dataArrays.Items[key.Index].Array, multiplier);
        }
    }
}

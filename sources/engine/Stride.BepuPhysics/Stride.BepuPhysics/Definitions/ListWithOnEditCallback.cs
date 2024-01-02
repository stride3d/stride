using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.Core;

namespace Stride.BepuPhysics.Definitions
{
    [DataContract(Inherited = true)]
    public class ListWithOnEditCallback<T> : List<T>
    {
        public Action? OnEditCallBack { get; internal set; }

        public new void Add(T item)
        {
            base.Add(item);
            OnEditCallBack?.Invoke();
        }
        public new void Remove(T item)
        {
            base.Remove(item);
            OnEditCallBack?.Invoke();
        }
        public new void RemoveAll(Predicate<T> match)
        {
            base.RemoveAll(match);
            OnEditCallBack?.Invoke();
        }
        public new void RemoveAt(int index)
        {
            base.RemoveAt(index);
            OnEditCallBack?.Invoke();
        }
        public new void RemoveRange(int index, int count)
        {
            base.RemoveRange(index, count);
            OnEditCallBack?.Invoke();
        }
        public new void AddRange(IEnumerable<T> collection)
        {
            base.AddRange(collection);
            OnEditCallBack?.Invoke();
        }
        public new void Clear()
        {
            base.Clear();
            OnEditCallBack?.Invoke();
        }
    }

    [DataContract]
    public sealed class ListOfContainer : List<BodyContainerComponent>
    {
        //i'm working
        public Action? OnEditCallBack { get; internal set; }

        public new void Add(BodyContainerComponent item)
        {
            base.Add(item);
            OnEditCallBack?.Invoke();
        }
        public new void Remove(BodyContainerComponent item)
        {
            base.Remove(item);
            OnEditCallBack?.Invoke();
        }
        public new void RemoveAll(Predicate<BodyContainerComponent> match)
        {
            base.RemoveAll(match);
            OnEditCallBack?.Invoke();
        }
        public new void RemoveAt(int index)
        {
            base.RemoveAt(index);
            OnEditCallBack?.Invoke();
        }
        public new void RemoveRange(int index, int count)
        {
            base.RemoveRange(index, count);
            OnEditCallBack?.Invoke();
        }
        public new void AddRange(IEnumerable<BodyContainerComponent> collection)
        {
            base.AddRange(collection);
            OnEditCallBack?.Invoke();
        }
        public new void Clear()
        {
            base.Clear();
            OnEditCallBack?.Invoke();
        }
    }

    [DataContract]
    public sealed class ListOfColliders : List<ColliderBase>
    {
        public Action? OnEditCallBack { get; internal set; }

        public new void Add(ColliderBase item)
        {
            base.Add(item);
            OnEditCallBack?.Invoke();
        }
        public new void Remove(ColliderBase item)
        {
            base.Remove(item);
            OnEditCallBack?.Invoke();
        }
        public new void RemoveAll(Predicate<ColliderBase> match)
        {
            base.RemoveAll(match);
            OnEditCallBack?.Invoke();
        }
        public new void RemoveAt(int index)
        {
            base.RemoveAt(index);
            OnEditCallBack?.Invoke();
        }
        public new void RemoveRange(int index, int count)
        {
            base.RemoveRange(index, count);
            OnEditCallBack?.Invoke();
        }
        public new void AddRange(IEnumerable<ColliderBase> collection)
        {
            base.AddRange(collection);
            OnEditCallBack?.Invoke();
        }
        public new void Clear()
        {
            base.Clear();
            OnEditCallBack?.Invoke();
        }
    }

}

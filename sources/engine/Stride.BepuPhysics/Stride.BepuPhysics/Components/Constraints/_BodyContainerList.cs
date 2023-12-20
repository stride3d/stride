using Stride.BepuPhysics.Components.Containers;
using Stride.Core;

namespace Stride.BepuPhysics.Components.Constraints
{
#warning maybe replace by stride impl
    [DataContract]
    public sealed class BodyContainerList : List<BodyContainerComponent>
    {
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
}

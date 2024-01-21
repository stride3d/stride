using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.Core;

namespace Stride.BepuPhysics.Definitions
{
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

using Stride.BepuPhysics.Definitions.Colliders;
using Stride.Core;

namespace Stride.BepuPhysics.Definitions
{
    [DataContract]
    internal sealed class ListOfColliders : List<ColliderBase>, IList<ColliderBase> // inherit from List<T> for serializer workaround but present users with `IList<T>` instead
    {
        internal required CompoundCollider Owner { get; set; }

        void ICollection<ColliderBase>.Add(ColliderBase item)
        {
            base.Add(item);
            Owner.OnEditCallBack.Invoke();
        }

        bool ICollection<ColliderBase>.Remove(ColliderBase item)
        {
            bool val = base.Remove(item);
            Owner.OnEditCallBack.Invoke();
            return val;
        }

        void ICollection<ColliderBase>.Clear()
        {
            base.Clear();
            Owner.OnEditCallBack.Invoke();
        }

        void IList<ColliderBase>.Insert(int index, ColliderBase item)
        {
            base.Insert(index, item);
            Owner.OnEditCallBack.Invoke();
        }

        void IList<ColliderBase>.RemoveAt(int index)
        {
            base.RemoveAt(index);
            Owner.OnEditCallBack.Invoke();
        }
    }
}

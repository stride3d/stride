namespace Stride.Updater.New;

internal sealed class RootMember<T>(UpdatableType<T> type) : UpdatableMember<object, T>, IUpdatableMember<RootMember<T>, object, T>
{
    public static bool SupportsByReference => false;

    public override string Name => "Root";

    public override UpdatableMember<T> CreateProperty(string name)
    {
        return type.CreateProperty(name);
    }

    public override UpdatableMember<T> CreateIndexer(string name)
    {
        return type.CreateIndexer(name);
    }

    public static T GetValue(RootMember<T> @this, object parent)
    {
        return (T)parent;
    }

    public override unsafe void Update(object parent, byte* data, UpdateObjectData[] updateObjects) => Update(this, parent, data, updateObjects);

    public override unsafe void Update(ref object parent, byte* data, UpdateObjectData[] updateObjects) => Update(this, ref parent, data, updateObjects);
}

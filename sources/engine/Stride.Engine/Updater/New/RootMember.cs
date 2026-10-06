namespace Stride.Updater.New;

internal sealed class RootMember<T>(UpdatableType<T> type) : UpdatableMember<object, T>
{
    protected override bool SupportsByReference => false;

    public override string Name => "Root";

    public override UpdatableMember<T> CreateProperty(string name)
    {
        return type.CreateProperty(name);
    }

    public override UpdatableMember<T> CreateIndexer(string name)
    {
        return type.CreateIndexer(name);
    }

    protected override T GetValue(object parent)
    {
        return (T)parent;
    }
}

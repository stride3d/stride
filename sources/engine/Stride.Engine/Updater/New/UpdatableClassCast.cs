namespace Stride.Updater.New;

internal sealed class UpdatableClassCast<TFrom, TTo>(string name, UpdatableType<TTo> type) : UpdatableMember<TFrom, TTo>
    where TTo : class
{
    protected override bool SupportsByReference => false;
    internal override bool IsCast => true;

    public override string Name { get; } = name;

    protected override TTo GetValue(TFrom instance)
    {
        return instance as TTo;
    }

    public override UpdatableMember<TTo> CreateProperty(string name)
    {
        return type.CreateProperty(name);
    }

    public override UpdatableMember<TTo> CreateIndexer(string name)
    {
        return type.CreateIndexer(name);
    }
}

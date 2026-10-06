namespace Stride.Updater.New;

internal sealed class UpdatableClassCast<TFrom, TTo>(UpdatableType<TTo> type) : UpdatableMember<TFrom, TTo>
    where TFrom : class
    where TTo : class, TFrom
{
    protected override bool SupportsByReference => false;

    public override string Name { get; } = $"({typeof(TTo).FullName})";

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

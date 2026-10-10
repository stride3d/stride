namespace Stride.Updater.New;

internal sealed class UpdatableClassCast<TFrom, TTo>(string name, UpdatableType<TTo> type) : UpdatableMember<TFrom, TTo>, IUpdatableMember<UpdatableClassCast<TFrom, TTo>, TFrom, TTo>
    where TTo : class
{
    public static bool SupportsByReference => false;
    internal override bool IsCast => true;

    public override string Name { get; } = name;

    public static TTo GetValue(UpdatableClassCast<TFrom, TTo> @this, TFrom instance)
    {
        return instance as TTo;
    }

    public override unsafe void Update(TFrom parent, byte* data, UpdateObjectData[] updateObjects) => Update(this, parent, data, updateObjects);

    public override unsafe void Update(ref TFrom parent, byte* data, UpdateObjectData[] updateObjects) => Update(this, ref parent, data, updateObjects);

    public override UpdatableMember<TTo> CreateProperty(string name)
    {
        return type.CreateProperty(name);
    }

    public override UpdatableMember<TTo> CreateIndexer(string name)
    {
        return type.CreateIndexer(name);
    }
}

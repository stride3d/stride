namespace Stride.BepuPhysics.Processors
{
    internal abstract class ConstraintDataBase
    {
        public abstract bool Exist { get; }

        internal abstract void RebuildConstraint();
        internal abstract void DestroyConstraint();
        internal abstract void TryUpdateDescription();
    }
}

internal sealed class EmptyInfo : SymbolInfo
{
    internal static readonly EmptyInfo Instance = new EmptyInfo();
    private EmptyInfo() { }
    internal override bool IsEmpty => true;
}

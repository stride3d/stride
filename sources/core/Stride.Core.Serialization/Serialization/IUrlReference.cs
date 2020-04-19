namespace Stride.Core.Serialization
{
    /// <summary>
    /// Represents a Url to an asset.
    /// </summary>
    public interface IUrlReference
    {
        // <summary>
        /// Gets the Url of the referenced asset.
        /// </summary>
        string Url { get; }

        /// <summary>
        /// Gets whether the is <c>null</c> or empty.
        /// </summary>
        bool IsEmpty { get; }
    }
}

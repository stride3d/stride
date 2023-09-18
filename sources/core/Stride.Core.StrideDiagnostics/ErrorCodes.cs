namespace Stride.Core.StrideDiagnostics;

public static class ErrorCodes
{
    /// <summary>
    /// An Array must have a public/internal getter for Serialization.
    /// </summary>
    public const string InvalidArrayAccess = "STRD001";

    /// <summary>
    /// A collection must have a public/internal getter for Serialization.
    /// </summary>
    public const string InvalidCollectionAccess = "STRD002";
    /// <summary>
    /// A property must have a public/internal getter.
    /// A property must be set during instantiation, then no public setter would be valid.
    /// Or it must have a public setter else.
    /// </summary>
    public const string InvalidPropertyAccess = "STRD003";
    /// <summary>
    /// For <see cref="System.Collections.Generic.Dictionary{TKey, TValue}"/> the only valid Keys are primitive Types.
    /// All complex types like custom structs, objects are not allowed.
    /// // TODO : Enums are supported or not?
    /// </summary>
    public const string InvalidDictionaryKey = "STRD004";
    /// <summary>
    /// It's invalid to Datamember a private property.
    /// Also its invalid to Datamember a property which has DataMemberIgnore.
    /// </summary>
    public const string InvalidDataMemberCombination = "STRD007";
}

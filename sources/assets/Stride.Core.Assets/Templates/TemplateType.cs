namespace Stride.Core.Assets.Templates;

/// <summary>The type of template, based on it's yaml tag.</summary>
public enum TemplateType
{
    /// <summary>A standard template that uses the default tag.</summary>
    Standard,
    /// <summary>A sample template that shows up in the create or open project dialogue in Game Studio.</summary>
    Sample,
    /// <summary>An asset pack template that users can select when creating a new Stride project to include additional assets.</summary>
    AssetPack
}

// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Constraints;
using Microsoft.TemplateEngine.Abstractions.Parameters;
using Microsoft.TemplateEngine.Edge.Settings;

namespace Stride.Assets.Editor.Tests.Helpers;

/// <summary>
/// A dotnet new template (<see cref="ITemplateInfo"/>) built in a test: its identity, name and parameters, and the host file
/// (dotnetcli.host.json) that hides some of them.
/// </summary>
public sealed class FakeTemplateInfo : ITemplateInfo, ITemplateInfoHostJsonCache
{
    public FakeTemplateInfo(string identity, string name, params ITemplateParameter[] parameters)
    {
        Identity = identity;
        Name = name;
        ParameterDefinitions = new ParameterDefinitionSet(parameters);
        ShortNameList = [identity];
    }

    /// <summary>
    /// The host file of the template, as the template engine caches it; null for none.
    /// </summary>
    public string? HostData { get; set; }

    public string? Author => "Tests";

    public string? Description { get; set; }

    public IReadOnlyList<string> Classifications => [];

    public string? DefaultName { get; set; }

    public string Identity { get; }

    public string? GroupIdentity => null;

    public int Precedence => 0;

    public string Name { get; }

    public IReadOnlyList<string> ShortNameList { get; }

    public IParameterDefinitionSet ParameterDefinitions { get; }

    public IReadOnlyDictionary<string, string> TagsCollection => new Dictionary<string, string>();

    public IReadOnlyDictionary<string, IBaselineInfo> BaselineInfo => new Dictionary<string, IBaselineInfo>();

    public IReadOnlyList<Guid> PostActions => [];

    public IReadOnlyList<TemplateConstraintInfo> Constraints => [];

    public string? ThirdPartyNotices => null;

    public bool PreferDefaultName => false;

    public Guid GeneratorId => Guid.Empty;

    public string MountPointUri => "fake://" + Identity;

    public string ConfigPlace => "/.template.config/template.json";

    public string? LocaleConfigPlace => null;

    public string? HostConfigPlace => null;

    public bool HasScriptRunningPostActions { get; set; }

#pragma warning disable CS0618 // The obsolete members of ITemplateInfo are still part of the interface
    public IReadOnlyDictionary<string, ICacheTag> Tags => new Dictionary<string, ICacheTag>();

    public IReadOnlyDictionary<string, ICacheParameter> CacheParameters => new Dictionary<string, ICacheParameter>();

    public IReadOnlyList<ITemplateParameter> Parameters => ParameterDefinitions;

    public string ShortName => ShortNameList[0];
#pragma warning restore CS0618
}

/// <summary>
/// A parameter of a <see cref="FakeTemplateInfo"/>.
/// </summary>
public sealed class FakeTemplateParameter : ITemplateParameter
{
    public FakeTemplateParameter(string name, string dataType = "string", string? defaultValue = null, string type = "parameter")
    {
        Name = name;
        DataType = dataType;
        DefaultValue = defaultValue;
        Type = type;
    }

    /// <summary>
    /// A bool parameter.
    /// </summary>
    public static FakeTemplateParameter Bool(string name, bool defaultValue, string? description = null)
        => new(name, "bool", defaultValue ? "true" : "false") { Description = description };

    /// <summary>
    /// A text parameter.
    /// </summary>
    public static FakeTemplateParameter Text(string name, string? defaultValue, string? description = null)
        => new(name, "string", defaultValue) { Description = description };

    /// <summary>
    /// A choice parameter, single or multiple.
    /// </summary>
    public static FakeTemplateParameter Choice(string name, string? defaultValue, bool allowMultipleValues, params string[] choices)
        => new(name, "choice", defaultValue)
        {
            AllowMultipleValues = allowMultipleValues,
            Choices = choices.ToDictionary(x => x, x => new ParameterChoice(null, null)),
        };

    /// <summary>
    /// The built-in name parameter, bound to the sourceName of the template.
    /// </summary>
    public static FakeTemplateParameter NameParameter()
        => new("name", "string", null) { IsName = true };

    public string Name { get; }

    public string? DisplayName => null;

    public string? Description { get; init; }

    public string? Documentation => null;

    public string Type { get; }

    public string DataType { get; }

    public string? DefaultValue { get; }

    public string? DefaultIfOptionWithoutValue => null;

    public TemplateParameterPrecedence Precedence => TemplateParameterPrecedence.Default;

#pragma warning disable CS0618 // Still part of the interface
    public TemplateParameterPriority Priority => TemplateParameterPriority.Optional;
#pragma warning restore CS0618

    public bool IsName { get; init; }

    public IReadOnlyDictionary<string, ParameterChoice>? Choices { get; init; }

    public bool AllowMultipleValues { get; init; }

    public bool Equals(ITemplateParameter? other) => other is not null && string.Equals(Name, other.Name, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is ITemplateParameter other && Equals(other);

    public override int GetHashCode() => Name.GetHashCode(StringComparison.Ordinal);
}

// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Text.RegularExpressions;
using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.Upgrade;

/// <summary>
/// What the upgrade steps know about a class: from its parsed code for a project class, from <see cref="EngineShaderClasses"/>
/// for an engine class.
/// </summary>
internal sealed class ShaderUpgradeClass
{
    public required string Name { get; init; }

    /// <summary>The file of a project class; null for an engine class.</summary>
    public string? Path { get; init; }

    public int GenericParameterCount { get; init; }

    public bool IsModule { get; init; }

    /// <summary>Declared <c>[Root]</c>.</summary>
    public bool IsRoot { get; init; }

    public List<string> Parents { get; init; } = [];

    /// <summary>Engine classes: used as the type of a <c>compose</c> declaration.</summary>
    public bool IsComposeType { get; init; }

    /// <summary>Types of the <c>compose</c> declarations of this class.</summary>
    public List<string> ComposeTypes { get; } = [];

    /// <summary>Members that have one value per instance: variables, streams, compositions and samplers that are not <c>module</c>.</summary>
    public List<string> InstanceMembers { get; } = [];

    /// <summary>Signature = the parameter types, comma-separated, without spaces.</summary>
    public List<(string Name, bool IsModule, bool IsOverride, bool IsAbstract, string Signature, string Body)> Methods { get; } = [];

    /// <summary>Classes named before a <c>.</c> in the code (static references), with the text written for them.</summary>
    public Dictionary<string, string> StaticReferences { get; } = [];

    public bool IsEngine => Path == null;
}

/// <summary>
/// The classes of a project (its <c>.sdsl</c> files) and of the engine, with the rules the upgrade steps share.
/// </summary>
internal sealed class ShaderUpgradeProject
{
    private readonly Dictionary<string, HashSet<string>> ancestors = [];
    private readonly Dictionary<string, bool> candidates = [];
    private readonly HashSet<string> composeTypes;
    private readonly HashSet<(string Owner, string Method)> overriddenByInstance = [];
    private readonly HashSet<string> imported = [];
    private readonly Dictionary<string, bool> needsRoot = [];
    private readonly HashSet<string> rootMarkers;
    private readonly Dictionary<string, bool> rootOnly = [];

    public ShaderUpgradeProject(IReadOnlyDictionary<string, string> files, EngineShaderClasses? engine, List<string> notes)
    {
        foreach (var engineClass in engine?.Classes ?? [])
            Classes[engineClass.Name] = engineClass;

        // The engine's [Root] classes by name, also when the files are the engine itself, written before the markers existed
        rootMarkers = (engine ?? EngineShaderClasses.Current).Classes.Where(x => x.IsRoot).Select(x => x.Name).ToHashSet();

        // A class that two files of the project declare: which one the build uses doesn't depend on its file name, so the upgrade decides
        // nothing for it, and reads it as unknown where it is used
        var ambiguous = new Dictionary<string, List<string>>();
        foreach (var (path, text) in files.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            // Parents written in the branches of `#if` the parsed code doesn't have are imported too
            imported.UnionWith(new ShaderSourceText(text).AllParentNames());

            foreach (var shaderClass in Parse(path, text, notes))
            {
                if (ambiguous.TryGetValue(shaderClass.Name, out var paths))
                {
                    paths.Add(path);
                    continue;
                }
                // A project class wins over an engine class of the same name
                if (Classes.TryGetValue(shaderClass.Name, out var existing) && !existing.IsEngine)
                {
                    ambiguous[shaderClass.Name] = [existing.Path!, path];
                    Classes.Remove(shaderClass.Name);
                    continue;
                }
                Classes[shaderClass.Name] = shaderClass;
                if (shaderClass.IsRoot)
                    rootMarkers.Add(shaderClass.Name);
            }
        }
        foreach (var (name, paths) in ambiguous)
            notes.Add($"{string.Join(", ", paths)}: class {name} is declared in each of these files, so the upgrade can't tell which one is used: it changes neither, and the classes that use {name} are upgraded as if it were unknown; keep one of them");

        composeTypes = Classes.Values.Where(x => x.IsComposeType).Select(x => x.Name)
            .Concat(Classes.Values.SelectMany(x => x.ComposeTypes))
            .ToHashSet();

        foreach (var shaderClass in ProjectClasses)
        {
            // C# can mix a shader into a composition node beside the compose type (TextureProjectionReceiverSpot in a light group):
            // any shader that is not root-only may be in a composition
            foreach (var method in shaderClass.Methods.Where(x => x.IsOverride && !x.IsModule && !IsRootOnly(shaderClass.Name)))
            {
                foreach (var ancestor in Ancestors(shaderClass.Name))
                {
                    if (Classes.TryGetValue(ancestor, out var owner) && owner.Methods.Any(x => x.Name == method.Name && !x.IsOverride && !x.IsModule))
                        overriddenByInstance.Add((ancestor, method.Name));
                }
            }

            foreach (var parent in shaderClass.Parents)
                imported.Add(parent);
            foreach (var reference in shaderClass.StaticReferences.Keys)
                imported.Add(reference);
        }
    }

    public Dictionary<string, ShaderUpgradeClass> Classes { get; } = [];

    public IEnumerable<ShaderUpgradeClass> ProjectClasses => Classes.Values.Where(x => !x.IsEngine);

    public HashSet<string> Ancestors(string name)
    {
        if (ancestors.TryGetValue(name, out var result))
            return result;
        ancestors[name] = result = [];
        if (Classes.TryGetValue(name, out var shaderClass))
        {
            foreach (var parent in shaderClass.Parents)
            {
                result.Add(parent);
                result.UnionWith(Ancestors(parent));
            }
        }
        return result;
    }

    /// <summary>
    /// Can have instances: it is the type of a <c>compose</c> declaration, or inherits one.
    /// </summary>
    public bool IsInstance(string name) => composeTypes.Contains(name) || Ancestors(name).Any(composeTypes.Contains);

    public bool IsComposeType(string name) => composeTypes.Contains(name);

    /// <summary>
    /// The members with one value per instance, of the class or its parents, that <paramref name="body"/> names.
    /// </summary>
    public IEnumerable<string> InstanceMembersNamed(string name, string body)
    {
        var members = Ancestors(name).Append(name).Where(x => Classes.TryGetValue(x, out var shaderClass) && !shaderClass.IsEngine)
            .SelectMany(x => Classes[x].InstanceMembers).ToHashSet();
        return Regex.Matches(body, @"(?<![\w.])[A-Za-z_]\w*").Select(x => x.Value).Where(members.Contains).Distinct();
    }

    /// <summary>
    /// Can be mixed only at the root, as the compiler decides it: <c>[Root]</c>, or a shader whose parent or import is root-only;
    /// a module passes on only its own <c>[Root]</c>. In code written before modules, every parent is a shader.
    /// </summary>
    public bool IsRootOnly(string name)
    {
        if (rootOnly.TryGetValue(name, out var result))
            return result;
        rootOnly[name] = false;
        result = rootMarkers.Contains(name)
            || Classes.TryGetValue(name, out var shaderClass) && !shaderClass.IsModule
                && shaderClass.Parents.Any(parent => StreamsModuleFor(name, parent) == null
                    && (Classes.TryGetValue(parent, out var parentClass) && parentClass.IsModule ? rootMarkers.Contains(parent) : IsRootOnly(parent)));
        return rootOnly[name] = result;
    }

    /// <summary>
    /// A shader that was root-only through a parent that becomes a module, and whose code relies on being mixed at the root, gets its
    /// own <c>[Root]</c>: a module passes on only its own, so without it the shader would be checked as a shader used in compositions
    /// (SDSL0204-0206). Relies on being root-only = its module code reads instance state of the shader or its shader parents, or calls
    /// an instance method, or it overrides a module method with a plain override. Shaders used in compositions don't get it.
    /// </summary>
    public bool NeedsRootMarker(string name)
    {
        if (needsRoot.TryGetValue(name, out var result))
            return result;
        needsRoot[name] = false;
        result = Classes.TryGetValue(name, out var shaderClass) && !shaderClass.IsEngine && !shaderClass.IsModule && !BecomesModule(name)
            && !IsInstance(name) && IsRootOnly(name) && !IsRootOnlyAfterUpgrade(name) && ReliesOnRoot(shaderClass);
        return needsRoot[name] = result;
    }

    // As the compiler decides it once the upgrade is done: module parents are imports, which pass on only their own [Root]
    private bool IsRootOnlyAfterUpgrade(string name)
        => rootMarkers.Contains(name)
            || Classes.TryGetValue(name, out var shaderClass) && !IsModuleAfterUpgrade(name)
                && shaderClass.Parents.Any(parent => StreamsModuleFor(name, parent) == null
                    && (IsModuleAfterUpgrade(parent) ? rootMarkers.Contains(parent) : IsRootOnlyAfterUpgrade(parent) || NeedsRootMarker(parent)));

    private bool ReliesOnRoot(ShaderUpgradeClass shaderClass)
    {
        var instanceClasses = Ancestors(shaderClass.Name).Append(shaderClass.Name)
            .Where(x => Classes.TryGetValue(x, out var c) && !c.IsEngine && !IsModuleAfterUpgrade(x))
            .Select(x => Classes[x]).ToList();
        var members = instanceClasses.SelectMany(x => x.InstanceMembers).ToHashSet();
        var methods = instanceClasses.SelectMany(x => x.Methods).Where(x => !x.IsModule && !x.IsOverride).Select(x => x.Name).ToHashSet();
        foreach (var method in shaderClass.Methods.Where(x => x.IsModule))
        {
            foreach (Match word in Regex.Matches(method.Body, @"(?<![\w.])([A-Za-z_]\w*)(\s*\()?"))
            {
                if (members.Contains(word.Groups[1].Value) || word.Groups[2].Success && methods.Contains(word.Groups[1].Value))
                    return true;
            }
        }
        // A plain override of a method that is module code (in a module, or marked module)
        return shaderClass.Methods.Where(x => x.IsOverride && !x.IsModule).Any(method => Ancestors(shaderClass.Name).Any(ancestor =>
            Classes.TryGetValue(ancestor, out var owner) && !owner.IsEngine && owner.Methods.Any(x => x.Name == method.Name && !x.IsOverride && (x.IsModule || IsModuleAfterUpgrade(ancestor)))));
    }

    // [Root] engine modules whose streams are also in a module that isn't [Root]
    private static readonly Dictionary<string, string> RootStreamModules = new()
    {
        ["ShaderBase"] = "ShaderBaseStream",
        ["ComputeShaderBase"] = "ComputeShaderStreams",
    };

    private static readonly string[] EntryPoints = ["VSMain", "HSMain", "DSMain", "GSMain", "PSMain", "CSMain"];

    /// <summary>
    /// The module a project shader used in compositions (it inherits a compose type) imports instead of its parent
    /// <paramref name="parent"/>, a [Root] engine module it inherited before 4.5 only to see its streams (it overrides no entry
    /// point); null otherwise. In a composition, the [Root] module is SDSL0119.
    /// </summary>
    public string? StreamsModuleFor(string name, string parent)
        => RootStreamModules.TryGetValue(parent, out var streams)
            && Classes.TryGetValue(name, out var shaderClass) && !shaderClass.IsEngine && !shaderClass.IsModule
            && shaderClass.Parents.Contains(parent) && IsInstance(name) && !shaderClass.Methods.Any(x => EntryPoints.Contains(x.Name))
            ? streams : null;

    /// <summary>
    /// A plain method (not <c>module</c>, not an override) of <paramref name="owner"/> that a shader which may be in a composition
    /// (not root-only) overrides with a plain override.
    /// </summary>
    public bool IsOverriddenByInstance(string owner, string method) => overriddenByInstance.Contains((owner, method));

    /// <summary>
    /// The class has no instance part: not used in compositions, no instance member, no plain method an instance class overrides,
    /// and its parents are the same. Engine classes: declared <c>module</c>.
    /// </summary>
    public bool CanBeModule(string name)
    {
        if (candidates.TryGetValue(name, out var result))
            return result;
        candidates[name] = false;
        if (!Classes.TryGetValue(name, out var shaderClass))
            return false;
        result = shaderClass.IsModule || (!shaderClass.IsEngine
            && !IsInstance(name)
            && shaderClass.InstanceMembers.Count == 0
            && !shaderClass.Methods.Any(x => !x.IsModule && !x.IsOverride && IsOverriddenByInstance(name, x.Name))
            && shaderClass.Parents.All(CanBeModule)
            && shaderClass.StaticReferences.Keys.Where(x => Classes.ContainsKey(x) && !Ancestors(name).Contains(x)).All(CanBeModule));
        return candidates[name] = result;
    }

    /// <summary>
    /// A project class that becomes <c>module X</c>: it can be one, and another class inherits it or refers to it by name.
    /// Classes nothing imports stay shaders (post effects, effect features).
    /// </summary>
    public bool BecomesModule(string name) => Classes.TryGetValue(name, out var shaderClass) && !shaderClass.IsEngine && !shaderClass.IsModule && imported.Contains(name) && CanBeModule(name);

    /// <summary>
    /// A module once the upgrade is done.
    /// </summary>
    public bool IsModuleAfterUpgrade(string name) => Classes.TryGetValue(name, out var shaderClass) && (shaderClass.IsModule || BecomesModule(name));

    /// <summary>
    /// The class declaring a method that is not an override, looking at <paramref name="name"/> then its parents in order.
    /// </summary>
    public ShaderUpgradeClass? MethodOwner(string name, string method)
    {
        if (!Classes.TryGetValue(name, out var shaderClass))
            return null;
        if (shaderClass.Methods.Any(x => x.Name == method && !x.IsOverride))
            return shaderClass;
        foreach (var parent in shaderClass.Parents)
        {
            if (MethodOwner(parent, method) is { } owner)
                return owner;
        }
        return null;
    }

    private static IEnumerable<ShaderUpgradeClass> Parse(string path, string text, List<string> notes)
    {
        string code;
        try
        {
            code = MonoGamePreProcessor.Run(text, System.IO.Path.GetFileName(path));
        }
        catch (Exception e)
        {
            notes.Add($"{path}: not upgraded, preprocessing failed: {e.Message}");
            yield break;
        }

        // An old spelling that a step of the upgrade changes is read as its new form (RecoverableInUpgrade)
        var result = SDSLParser.Parse(code, upgrade: true);
        if (result.AST is not ShaderFile file || result.Errors.Count > 0)
        {
            notes.Add($"{path}: not upgraded, parsing failed: {(result.Errors.Count > 0 ? result.Errors[0].Message : "no code")}");
            yield break;
        }
        foreach (var recovered in result.Recovered)
            notes.Add($"{path}: read past an old spelling; if it is still there after the upgrade, change it by hand: {recovered.Message}");

        foreach (var declaration in file.RootDeclarations.Concat(file.Namespaces.SelectMany(x => x.Declarations)).OfType<ShaderClass>())
        {
            var shaderClass = new ShaderUpgradeClass
            {
                Name = declaration.Name.Name,
                Path = path,
                GenericParameterCount = declaration.Generics?.Parameters.Count ?? 0,
                IsModule = declaration.IsModule,
                IsRoot = declaration.FindAttribute("Root") != null,
                Parents = declaration.Mixins.Select(x => BaseName(x.Name))
                    .Concat(declaration.Elements.OfType<ShaderImport>().Select(x => BaseName(x.Module.Name)))
                    .ToList(),
            };

            foreach (var element in declaration.Elements)
            {
                switch (element)
                {
                    case ShaderMember member:
                        AddMember(member);
                        break;
                    case ShaderBuffer buffer:
                        foreach (var member in buffer.Members)
                            AddMember(member);
                        break;
                    case ShaderCompose compose:
                        shaderClass.ComposeTypes.Add(BaseName(compose.Shader.Name));
                        if (!compose.IsStaged && !declaration.IsModule)
                            shaderClass.InstanceMembers.Add(compose.Name.Name);
                        break;
                    case ShaderSamplerState sampler:
                        if (!sampler.IsStaged && !declaration.IsModule)
                            shaderClass.InstanceMembers.Add(sampler.Name.Name);
                        break;
                    case ShaderMethod method:
                        var signature = string.Join(",", method.Parameters.Select(x => Regex.Replace(x.TypeName.ToString(), @"\s+", "")));
                        shaderClass.Methods.Add((method.Name.Name, method.IsStaged || declaration.IsModule, method.IsOverride, method.IsAbstract, signature, method.Body?.Info.Text.ToString() ?? ""));
                        break;
                }
            }

            // Strings and comments left out: `[Link("Class.Member")]` is not a reference
            var classCode = Regex.Replace(declaration.Info.Text.ToString(), "\"[^\"\\n]*\"|//[^\\n]*|/\\*.*?\\*/", x => x.Value[0] == '"' ? "\"\"" : " ", RegexOptions.Singleline);
            foreach (Match match in Regex.Matches(classCode, @"(?<![\w.])([A-Za-z_]\w*)\s*(<[^;{}()=]*>)?\s*\.\s*[A-Za-z_]"))
            {
                var name = match.Groups[1].Value;
                // A member, parameter or local variable of that name is not a reference either
                if (name is not ("streams" or "this" or "base") && name != shaderClass.Name
                    && !Regex.IsMatch(classCode, $@"(?:>|\]|\b(?!import\b|return\b|case\b)\w+)\s+{name}\s*[=;,)\[:]"))
                    shaderClass.StaticReferences.TryAdd(name, match.Value[..match.Value.LastIndexOf('.')].TrimEnd());
            }

            yield return shaderClass;

            void AddMember(ShaderMember member)
            {
                if (member.IsCompose)
                {
                    shaderClass.ComposeTypes.Add(BaseName(member.TypeName.Name));
                    if (!member.IsStaged && !declaration.IsModule)
                        shaderClass.InstanceMembers.Add(member.Name.Name);
                }
                else if (member.TypeModifier != TypeModifier.Const && !member.IsStaged && !declaration.IsModule)
                {
                    shaderClass.InstanceMembers.Add(member.Name.Name);
                }
            }
        }
    }

    public static string BaseName(string name)
    {
        var genericStart = name.IndexOf('<');
        return (genericStart < 0 ? name : name[..genericStart]).TrimEnd('[', ']', ' ');
    }
}

// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Text.RegularExpressions;

namespace Stride.Shaders.Parsing.Upgrade;

/// <summary>
/// Rewrites the <c>.sdsl</c> files of a project written for an older Stride version. Each syntax change adds a step, and running the
/// steps again on their own output changes nothing. Only the text steps leave newer code alone: the class steps assume code written
/// before their version, so a project upgrade runs them only on a project below it.
/// </summary>
public static class ShaderUpgrader
{
    /// <summary>
    /// The steps, in order.
    /// </summary>
    public static IReadOnlyList<string> Steps { get; } =
    [
        // 4.5: a class declared with `class`, an old spelling of `shader`, is declared with `shader`
        "class-to-shader",
        // 4.5: the `clone` method modifier has no effect since 4.4 (SDSL0212)
        "drop-clone",
        // 4.5: `stage` is now spelled `module`
        "stage-to-module",
        // 4.4: an implementation of an abstract method is marked `override` (SDSL0114); after the rename, the parser rejects `stage`
        "add-override",
        // 4.5: module code of a class used in compositions can only call module methods
        "module-helpers",
        // 4.5: classes with no instance part that other classes import are `module X`, imported with `import X;`
        "module-units",
    ];

    /// <summary>
    /// Runs the steps up to <paramref name="lastStep"/> (all by default) on <paramref name="files"/> (path → text).
    /// </summary>
    /// <param name="engine">The engine classes the project uses (<see cref="EngineShaderClasses.Current"/>); null when the files are the engine.</param>
    /// <param name="steps">The steps to run (all by default), in the order of <see cref="Steps"/>: the steps of one gate of the project upgrade.</param>
    public static ShaderUpgradeResult Upgrade(IReadOnlyDictionary<string, string> files, EngineShaderClasses? engine, string? lastStep = null, IReadOnlyCollection<string>? steps = null)
    {
        var texts = new Dictionary<string, string>(files);
        var notes = new List<string>();
        var stepCount = lastStep == null ? Steps.Count : Steps.ToList().IndexOf(lastStep) + 1;
        if (stepCount == 0)
            throw new ArgumentException($"Unknown step {lastStep}", nameof(lastStep));
        bool Runs(int index) => index < stepCount && (steps == null || steps.Contains(Steps[index]));

        if (Runs(0))
        {
            foreach (var path in texts.Keys.ToList())
                texts[path] = ClassToShader(texts[path]);
        }
        if (Runs(1))
        {
            foreach (var path in texts.Keys.ToList())
                texts[path] = DropClone(texts[path]);
        }
        if (Runs(2))
        {
            foreach (var path in texts.Keys.ToList())
                texts[path] = StageToModule(texts[path]);
        }
        if (Runs(3))
            Apply(texts, project => AddOverride(project, texts, notes), engine, notes);

        if (Runs(4))
            Apply(texts, project => MarkModuleHelpers(project, texts, notes), engine, notes);
        if (Runs(5))
            Apply(texts, project => ConvertModules(project, texts, notes), engine, notes);

        return new ShaderUpgradeResult(texts, notes.Distinct().ToList());
    }

    private static void Apply(Dictionary<string, string> texts, Func<ShaderUpgradeProject, Dictionary<string, ShaderSourceText>> step, EngineShaderClasses? engine, List<string> notes)
    {
        var project = new ShaderUpgradeProject(texts, engine, notes);
        foreach (var (path, text) in step(project))
        {
            if (text.HasEdits)
                texts[path] = text.Apply();
        }
    }

    /// <summary>
    /// <c>class</c> → <c>shader</c> where it declares a class: outside every brace but those of a <c>namespace</c>, not in comments or
    /// strings. A <c>class</c> inside a class is HLSL's, and stays.
    /// </summary>
    internal static string ClassToShader(string text)
    {
        var source = new ShaderSourceText(text);
        foreach (var match in source.CodeMatches(@"(?<![\w.])class(?=\s+[A-Za-z_])"))
        {
            if (IsInNamespacesOnly(source, match.Index))
                source.Replace(match.Index, match.Length, "shader");
        }
        return source.Apply();
    }

    private static bool IsInNamespacesOnly(ShaderSourceText source, int position)
    {
        var isNamespace = new Stack<bool>();
        for (var i = 0; i < position; i++)
        {
            if (!source.IsCode[i])
                continue;
            if (source.Text[i] == '{')
                isNamespace.Push(Regex.IsMatch(source.Text[..i], @"(?<![\w.])namespace\s+[\w.]+\s*$"));
            else if (source.Text[i] == '}' && isNamespace.Count > 0)
                isNamespace.Pop();
        }
        return isNamespace.All(x => x);
    }

    /// <summary>
    /// The <c>clone</c> method modifier is removed, not in comments or strings. An effect's <c>mixin clone</c> is left as it is.
    /// </summary>
    internal static string DropClone(string text)
    {
        var source = new ShaderSourceText(text);
        foreach (var match in source.CodeMatches(@"(?<![\w.])(?<!\bmixin\s+)clone\s+(?=[A-Za-z_])"))
            source.Replace(match.Index, match.Length, "");
        return source.Apply();
    }

    /// <summary>
    /// A method that implements an abstract method of a project class (same name and parameter types) without <c>override</c> gets it.
    /// Abstract methods of engine classes are not known here (the engine list has no methods): the compiler reports those (SDSL0114).
    /// </summary>
    private static Dictionary<string, ShaderSourceText> AddOverride(ShaderUpgradeProject project, Dictionary<string, string> texts, List<string> notes)
    {
        var sources = new Dictionary<string, ShaderSourceText>();
        foreach (var shaderClass in project.ProjectClasses)
        {
            foreach (var method in shaderClass.Methods.Where(x => !x.IsOverride && !x.IsAbstract))
            {
                var implements = project.Ancestors(shaderClass.Name).Any(ancestor => project.Classes.TryGetValue(ancestor, out var owner)
                    && owner.Methods.Any(x => x.IsAbstract && x.Name == method.Name && x.Signature == method.Signature));
                if (!implements)
                    continue;
                if (shaderClass.Methods.Count(x => x.Name == method.Name) > 1)
                {
                    notes.Add($"{shaderClass.Path}: {shaderClass.Name}.{method.Name} implements an abstract method but has overloads; add 'override' by hand");
                    continue;
                }

                var source = Source(sources, texts, shaderClass.Path!);
                foreach (var classText in source.FindClass(shaderClass.Name))
                {
                    foreach (var declaration in MethodDeclarations(source, classText, method.Name, "override"))
                        source.Insert(declaration, "override ");
                }
            }
        }
        return sources;
    }

    /// <summary>
    /// <c>stage</c> → <c>module</c> where it is a modifier (followed by another word), not in comments or strings.
    /// </summary>
    internal static string StageToModule(string text)
    {
        var source = new ShaderSourceText(text);
        foreach (var match in source.CodeMatches(@"(?<![\w.])stage(?=\s+[A-Za-z_])"))
            source.Replace(match.Index, match.Length, "module");
        return source.Apply();
    }

    /// <summary>
    /// In a class used in compositions, module code can't call an instance method: a plain method called from module code of the
    /// class or its parents gets <c>module</c>, and so do its overrides.
    /// </summary>
    private static Dictionary<string, ShaderSourceText> MarkModuleHelpers(ShaderUpgradeProject project, Dictionary<string, string> texts, List<string> notes)
    {
        var marked = new HashSet<(string Owner, string Method)>();
        var pending = new Stack<(string Context, string Caller, string Body)>();
        foreach (var shaderClass in project.ProjectClasses.Where(x => project.IsInstance(x.Name) && !x.IsModule))
        {
            foreach (var method in shaderClass.Methods.Where(x => x.IsModule))
                pending.Push((shaderClass.Name, $"{shaderClass.Name}.{method.Name}", method.Body));
        }

        while (pending.Count > 0)
        {
            var (context, caller, body) = pending.Pop();
            foreach (Match call in Regex.Matches(body, @"(?<![\w.])(?:(?:this|base)\s*\.\s*)?([A-Za-z_]\w*)\s*\("))
            {
                var name = call.Groups[1].Value;
                if (project.MethodOwner(context, name) is not { IsEngine: false } owner)
                    continue;
                var target = owner.Methods.First(x => x.Name == name && !x.IsOverride);
                if (target.IsModule || project.BecomesModule(owner.Name))
                    continue;
                // A method of a compose type is called through slots, one chain per instance: it can't be module
                if (project.IsComposeType(owner.Name))
                {
                    notes.Add($"{owner.Path}: module code {caller} calls {owner.Name}.{name}, an instance method; this works only if the class of {caller} is mixed at the root, not used in a composition");
                    continue;
                }
                if (marked.Add((owner.Name, name)))
                {
                    // No spelling allows module code to read a member that has one value per instance
                    if (project.InstanceMembersNamed(owner.Name, target.Body).FirstOrDefault() is { } member)
                        notes.Add($"{owner.Path}: {owner.Name}.{name} becomes module, because module code calls it ({caller}), but it reads {member}, which has one value per instance; in a composition, that fails: make {member} module, or don't call {name} from module code");
                    pending.Push((owner.Name, $"{owner.Name}.{name}", target.Body));
                }
            }
        }

        // Overrides of a marked method are module code too
        foreach (var shaderClass in project.ProjectClasses)
        {
            foreach (var method in shaderClass.Methods.Where(x => x.IsOverride && !x.IsModule))
            {
                if (project.Ancestors(shaderClass.Name).Any(x => marked.Contains((x, method.Name))))
                    marked.Add((shaderClass.Name, method.Name));
            }
        }

        var sources = new Dictionary<string, ShaderSourceText>();
        foreach (var (owner, method) in marked)
        {
            var shaderClass = project.Classes[owner];
            var source = Source(sources, texts, shaderClass.Path!);
            foreach (var classText in source.FindClass(owner))
            {
                foreach (var declaration in MethodDeclarations(source, classText, method, "module"))
                    source.Insert(declaration, "module ");
            }
        }
        return sources;
    }

    /// <summary>
    /// A class with no instance part that another class imports (inherits it or refers to it by name) becomes <c>module X</c>,
    /// with no <c>module</c> modifier inside; a parent that is a module becomes <c>import X;</c> in the body; a class used by
    /// name that is a shader is added to the parents, which is what the compiler did silently.
    /// </summary>
    private static Dictionary<string, ShaderSourceText> ConvertModules(ShaderUpgradeProject project, Dictionary<string, string> texts, List<string> notes)
    {
        var sources = new Dictionary<string, ShaderSourceText>();
        foreach (var shaderClass in project.ProjectClasses)
        {
            var source = Source(sources, texts, shaderClass.Path!);
            var declarations = source.FindClass(shaderClass.Name).ToList();
            if (declarations.Count == 0)
                notes.Add($"{shaderClass.Path}: class {shaderClass.Name} not found in the text, not upgraded");
            foreach (var classText in declarations)
                ConvertDeclaration(project, shaderClass, source, classText, notes);
        }
        return sources;
    }

    private static void ConvertDeclaration(ShaderUpgradeProject project, ShaderUpgradeClass shaderClass, ShaderSourceText source, ShaderClassText classText, List<string> notes)
    {
        // A shader that relies on being mixed at the root, root-only before through a parent that becomes a module
        if (project.NeedsRootMarker(shaderClass.Name))
            InsertRootAttribute(source, classText);

        if (project.BecomesModule(shaderClass.Name))
        {
            source.Replace(classText.Keyword, classText.KeywordText.Length, "module");
            foreach (var modifier in source.CodeMatches(@"(?<![\w.])module\s+(?=[A-Za-z_])", classText.Open, classText.Close))
                source.Replace(modifier.Index, modifier.Length, "");
        }
        else
        {
            RemoveBaseFromModuleCalls(project, shaderClass, source, classText, notes);
        }

        var (colon, parents) = source.ParseParents(classText);
        // A shader used in compositions sees the streams of a [Root] module through its streams module (SDSL0119 otherwise)
        var imports = new List<string>();
        foreach (var parent in parents.Where(x => project.IsModuleAfterUpgrade(x.Name)))
        {
            if (project.StreamsModuleFor(shaderClass.Name, parent.Name) is { } streams)
            {
                notes.Add($"{shaderClass.Path}: {shaderClass.Name} is used in compositions: it imports {streams} instead of {parent.Name} ([Root])");
                imports.Add(streams);
            }
            else
            {
                imports.Add(parent.Text);
            }
        }
        var kept = parents.Where(x => !project.IsModuleAfterUpgrade(x.Name)).Select(x => x.Text).ToList();

        var ancestors = project.Ancestors(shaderClass.Name);
        var added = shaderClass.StaticReferences
            .Where(x => project.Classes.ContainsKey(x.Key) && !project.IsModuleAfterUpgrade(x.Key) && !ancestors.Contains(x.Key))
            .Select(x => x.Value)
            .ToList();

        if (imports.Count == 0 && added.Count == 0)
            return;

        // Parents
        var newParents = kept.Concat(added).ToList();
        if (colon >= 0 && parents.Count > 0)
        {
            var removeStart = colon;
            while (removeStart > classText.NameEnd && char.IsWhiteSpace(source.Text[removeStart - 1]))
                removeStart--;
            source.Replace(removeStart, parents[^1].End - removeStart, newParents.Count > 0 ? $" : {string.Join(", ", newParents)}" : "");
        }
        else if (newParents.Count > 0)
        {
            var headerEnd = classText.Open;
            while (headerEnd > classText.NameEnd && char.IsWhiteSpace(source.Text[headerEnd - 1]))
                headerEnd--;
            source.Insert(headerEnd, $" : {string.Join(", ", newParents)}");
        }

        // Imports, first in the body
        if (imports.Count > 0)
        {
            var newLine = source.NewLine;
            var lineEnd = source.Text.IndexOf('\n', classText.Open);
            if (lineEnd >= 0 && lineEnd < classText.Close && source.Text[(classText.Open + 1)..lineEnd].Trim().Length == 0)
            {
                var indentation = source.BodyIndentation(classText.Open);
                var nextLineEnd = source.Text.IndexOf('\n', lineEnd + 1);
                var nextLine = source.Text[(lineEnd + 1)..(nextLineEnd < 0 ? source.Text.Length : nextLineEnd)].Trim();
                var separator = nextLine.Length > 0 && !nextLine.StartsWith('}') && !nextLine.StartsWith("import ") ? newLine : "";
                source.Insert(lineEnd + 1, string.Concat(imports.Select(x => $"{indentation}import {x};{newLine}")) + separator);
            }
            else
            {
                source.Insert(classText.Open + 1, string.Concat(imports.Select(x => $" import {x};")));
            }
        }
    }

    // Before the declaration (attributes come before `internal` and `partial`): on its own line when the declaration starts its line
    private static void InsertRootAttribute(ShaderSourceText source, ShaderClassText classText)
    {
        var lineStart = source.Text.LastIndexOf('\n', Math.Max(classText.Keyword - 1, 0)) + 1;
        var start = lineStart + Regex.Match(source.Text[lineStart..classText.Keyword], @"(?:(?:internal|partial)\s+)*$").Index;
        var indentation = source.Text[lineStart..start];
        if (indentation.Trim().Length == 0)
            source.Insert(lineStart, $"{indentation}[Root]{source.NewLine}");
        else
            source.Insert(start, "[Root] ");
    }

    /// <summary>
    /// <c>base.X()</c> where <c>X</c> is a method of a parent that becomes a module: from instance code it is an error in a composition
    /// (SDSL0208), and it means the same as <c>X()</c> when nothing overrides <c>X</c>, so <c>base.</c> is removed. Before 4.5, <c>base.X()</c>
    /// ran the version of the class mixed just before the caller, and <c>X()</c> the last one: an override in any class, a library's too, can
    /// make them differ.
    /// </summary>
    private static void RemoveBaseFromModuleCalls(ShaderUpgradeProject project, ShaderUpgradeClass shaderClass, ShaderSourceText source, ShaderClassText classText, List<string> notes)
    {
        foreach (var call in source.CodeMatches(@"(?<![\w.])base\s*\.\s*([A-Za-z_]\w*)\s*\(", classText.Open, classText.Close))
        {
            var name = call.Groups[1].Value;
            if (shaderClass.Methods.Any(x => x.Name == name))
                continue;
            var owner = shaderClass.Parents.Select(parent => project.MethodOwner(parent, name)).FirstOrDefault(x => x != null);
            if (owner == null || owner.IsEngine || !project.IsModuleAfterUpgrade(owner.Name))
                continue;
            if (project.Classes.Values.Any(x => !x.IsEngine && x.Methods.Any(m => m.Name == name && m.IsOverride)))
            {
                notes.Add($"{shaderClass.Path}: {shaderClass.Name} calls base.{name}() of the module {owner.Name}, and {name} is overridden: check the call by hand");
                continue;
            }
            source.Replace(call.Index, call.Groups[1].Index - call.Index, "");
        }
    }

    private static ShaderSourceText Source(Dictionary<string, ShaderSourceText> sources, Dictionary<string, string> texts, string path)
    {
        if (!sources.TryGetValue(path, out var source))
            sources[path] = source = new ShaderSourceText(texts[path]);
        return source;
    }

    /// <summary>
    /// Where to insert <paramref name="modifier"/> for each declaration of <paramref name="method"/> in the class body that doesn't have
    /// it: the start of the declaration (after its attributes).
    /// </summary>
    private static IEnumerable<int> MethodDeclarations(ShaderSourceText source, ShaderClassText classText, string method, string modifier)
    {
        foreach (var match in source.CodeMatches($@"(?<![\w.]){Regex.Escape(method)}\s*\(", classText.Open, classText.Close))
        {
            if (source.Depth(classText.Open, match.Index) != 1)
                continue;

            // The declaration starts after the previous `;`, `{`, `}` or attribute
            var start = match.Index;
            while (start > classText.Open && !(source.IsCode[start - 1] && source.Text[start - 1] is ';' or '{' or '}' or ']'))
                start--;
            while (start < match.Index)
            {
                if (char.IsWhiteSpace(source.Text[start]) || !source.IsCode[start])
                {
                    start++;
                }
                // A preprocessor line before the declaration
                else if (source.Text[start] == '#')
                {
                    var lineEnd = source.Text.IndexOf('\n', start);
                    start = lineEnd < 0 || lineEnd >= match.Index ? match.Index : lineEnd + 1;
                }
                else
                {
                    break;
                }
            }

            // Already has the modifier, or not a declaration (no return type before the name)
            var declaration = source.Text[start..match.Index];
            if (Regex.IsMatch(declaration, $@"(?<![\w.]){modifier}(?!\w)") || !Regex.IsMatch(declaration, @"[\w>\]]\s+$"))
                continue;
            yield return start;
        }
    }
}

/// <param name="Files">Every file (path → text), upgraded.</param>
/// <param name="Notes">What the upgrade couldn't do or decide, to show to the user.</param>
public sealed record ShaderUpgradeResult(IReadOnlyDictionary<string, string> Files, IReadOnlyList<string> Notes);

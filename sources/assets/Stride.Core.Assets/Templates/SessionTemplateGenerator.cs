// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.IO;
using System.Threading.Tasks;
using Stride.Core.IO;

namespace Stride.Core.Assets.Templates;

/// <summary>
/// Session-aware template generator: <see cref="Run"/> sequences
/// <see cref="Generate"/> → <see cref="ApplyMetadata"/> → <see cref="SaveSession"/> →
/// <see cref="AfterSave"/>. Each step is a public virtual so a decorator (e.g. Quantum-aware
/// editor wrapper) can intercept it; CLI / headless flows get bare no-op + plain
/// <see cref="PackageSession.Save"/>.
/// </summary>
public abstract class SessionTemplateGenerator : TemplateGeneratorBase<SessionTemplateGeneratorParameters>
{
    // TODO: move .gitignore content into an external file
    private static readonly string GitIgnore = @"
*.user
*.lock
*.lock.json
.vs/
_ReSharper*
*.suo
*.VC.db
*.vshost.exe
*.manifest
*.sdf
[Bb]in/
obj/
Cache/
"; //sadly templates and new games are different, the first ones are basically a copy the second is more programatically, so our best bet to have something easy to mantain is this for now.

    public sealed override bool Run(SessionTemplateGeneratorParameters parameters)
    {
        if (!Generate(parameters))
            return false;

        ApplyMetadata(parameters);

        SaveSession(parameters);

        parameters.Logger.Verbose("Compiling game assemblies...");
        parameters.Session.UpdateAssemblyReferences(parameters.Logger);
        parameters.Logger.Verbose("Game assemblies compiled...");

        return AfterSave(parameters).Result;
    }

    /// <summary>Generates the template; must work unattended.</summary>
    public abstract bool Generate(SessionTemplateGeneratorParameters parameters);

    /// <summary>No-op by default; Quantum decorator materializes override metadata into the asset graph.</summary>
    public virtual void ApplyMetadata(SessionTemplateGeneratorParameters parameters)
    {
    }

    /// <summary>Bare <see cref="PackageSession.Save"/>; Quantum decorator runs <c>PrepareForSave</c> first.</summary>
    public virtual void SaveSession(SessionTemplateGeneratorParameters parameters)
    {
        // A template generator must only write files under its output directory. Dependency packages
        // can be dirtied as a side effect of loading (e.g. an asset migration on a dev-source engine
        // .sdpkg); only save packages located under the output directory so those are left untouched.
        var outputDir = parameters.OutputDirectory.ToOSPath();
        if (outputDir.Length > 0 && outputDir[^1] != Path.DirectorySeparatorChar && outputDir[^1] != Path.AltDirectorySeparatorChar)
            outputDir += Path.DirectorySeparatorChar;
        var saveParameters = PackageSaveParameters.Default();
        saveParameters.PackageFilter = package =>
            package.FullPath is not null &&
            package.FullPath.ToOSPath().StartsWith(outputDir, StringComparison.OrdinalIgnoreCase);

        parameters.Session.DependencyManager.BeginSavingSession();
        parameters.Session.SourceTracker.BeginSavingSession();
        parameters.Session.Save(parameters.Logger, saveParameters);
        parameters.Session.SourceTracker.EndSavingSession();
        parameters.Session.DependencyManager.EndSavingSession();
    }

    /// <summary>Optional post-save step.</summary>
    public virtual Task<bool> AfterSave(SessionTemplateGeneratorParameters parameters)
    {
        return Task.FromResult(true);
    }

    protected void WriteGitIgnore(SessionTemplateGeneratorParameters parameters)
    {
        var fileName = UFile.Combine(parameters.OutputDirectory, ".gitignore");
        File.WriteAllText(fileName.ToOSPath(), GitIgnore);
    }
}

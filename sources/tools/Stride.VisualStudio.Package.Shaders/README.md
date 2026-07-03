# Stride.VisualStudio.Package.Shaders

A small, content-only **classic** VSIX that gives Visual Studio **instant** syntax highlighting for Stride
shader files (`.sdsl` / `.sdfx`).

## Why a separate VSIX

The main extension, `Stride.VisualStudio.Package`, is an out-of-process VisualStudio.Extensibility extension.
That model **cannot register a TextMate grammar** — grammar registration needs a classic in-process VSIX with a
`Microsoft.VisualStudio.VsPackage` pkgdef asset in its manifest, which the out-of-process manifest can't carry.
So this companion (net472, VSSDK) does just that one job. It ships no code — only:

- `Grammars/sdsl.tmLanguage.json` — the grammar (generated, see below).
- `sdsl-language-configuration.json` — comment/bracket/auto-close behavior.
- `stride-shaders.pkgdef` — registers the grammar + language configuration with VS's TextMate engine.
- `source.extension.vsixmanifest` — declares the pkgdef as a `VsPackage` asset (the piece that makes VS
  actually process it).

Instant/offline coloring is why this exists rather than relying on the (planned) LSP: an LSP colors via async
semantic tokens, which lag while typing. This grammar is the synchronous baseline; the LSP can refine on top.

## Regenerating the grammar

`Grammars/sdsl.tmLanguage.json` is generated from Stride's shader vocabulary (`Reserved.cs`) so it never drifts
from the language. It requires a built `Stride.Shaders.Parsers.dll` (any recent engine build produces one):

```
dotnet run generate-sdsl-grammar.cs
```

It auto-locates the newest `net10.0` build of the parser under `sources/shaders/Stride.Shaders.Parsers/bin`, or
takes an explicit path: `dotnet run generate-sdsl-grammar.cs -- <path-to-Stride.Shaders.Parsers.dll>`. Commit
the regenerated grammar.

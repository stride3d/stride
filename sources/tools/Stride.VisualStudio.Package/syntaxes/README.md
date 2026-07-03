# SDSL TextMate grammar (not used yet — kept for future use)

`sdsl.tmLanguage.json` is a TextMate grammar that colorizes Stride shader files (`.sdsl`/`.sdfx`). It is
**generated** from Stride's own shader vocabulary by `generate-sdsl-grammar.cs`, so the keyword/type lists never
drift from the language.

## Status: not wired into anything

This grammar is **not currently consumed** by any shipping component. It is not packed into this Visual Studio
extension, because the out-of-process VisualStudio.Extensibility model has no way to register a TextMate grammar
— that requires a classic (in-proc) VSIX pkgdef asset, which the generated out-of-process manifest can't carry.
It's committed here so the work isn't lost and is ready to adopt.

## Intended future use

- **VS Code extension** — drop this file in and reference it from `package.json` (`contributes.grammars`); coloring
  is instant and trivial there.
- **Visual Studio** — coloring will come from an **LSP** (`LanguageServerProvider`, which *is* supported
  out-of-process) via semantic tokens; this grammar can serve as the fast offline baseline if paired with a
  classic language-configuration VSIX later.

Before adopting it, regenerate to pick up any vocabulary changes (see below), and re-add a freshness check
(build the parser, regenerate, `git diff`) to whatever consumes it.

## Regenerating

Requires a built `Stride.Shaders.Parsers.dll` (any recent engine build produces one). Then:

```
dotnet run generate-sdsl-grammar.cs
```

It auto-locates the newest `net10.0` build of the parser under `sources/shaders/Stride.Shaders.Parsers/bin`, or
takes an explicit path: `dotnet run generate-sdsl-grammar.cs -- <path-to-Stride.Shaders.Parsers.dll>`.

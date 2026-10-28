# Localization

Stride uses GNU gettext PO/POT files for UI string translation. This folder contains the translation templates (`.pot`) and per-language catalogues (`.po`), as well as the script that regenerates them from source.

## Folder structure

```
localization/
  extract_strings.bat          # regenerates .pot files and updates .po files
  Stride.Core.Presentation.pot
  Stride.Assets.Presentation.pot
  Stride.Core.Assets.Editor.pot
  Stride.GameStudio.pot
  fr/
    Stride.Core.Presentation.fr.po
    ...
  ja/ de/ es/ ...              # one folder per language
```

Each `.pot` file is a translation template for one assembly domain. `.po` files are the translated catalogues; they live in `<language-code>/` subfolders and follow the naming pattern `<domain>.<language>.po`.

## Updating translation templates

Run `extract_strings.bat` from the repository root or from this folder:

```bat
sources\localization\extract_strings.bat
```

The script:
1. Builds the extractor from `sources/tools/Stride.Core.Translation.Extractor` (must already be compiled — see below).
2. Scans the relevant source directories for translatable strings.
3. Writes/overwrites the `.pot` files in this folder.
4. Runs `msgmerge` to propagate new strings into every existing `.po` file while preserving existing translations.

### Building the extractor

```bat
dotnet build sources\tools\Stride.Core.Translation.Extractor -c Debug
```

The script looks for the binary at `bin\Debug\net10.0\Stride.Core.Translation.Extractor.exe`. The tool runs cross-platform (no WPF dependency).

## Adding translatable strings

### C\#

Use the `Tr` shorthand (from `Stride.Core.Translation`):

| Call | Meaning |
|------|---------|
| `Tr._("Hello")` | Simple string |
| `Tr._p("Context", "Hello")` | String with message context |
| `Tr._n("One item", "{0} items")` | Singular / plural |
| `Tr._pn("Context", "One item", "{0} items")` | Context + plural |

The full `TranslationManager` API (`GetString`, `GetParticularString`, `GetPluralString`, `GetParticularPluralString`) is also extracted.

The `[Translation]` attribute marks enum values or other constants:

```csharp
[Translation("Save file")]
[Translation("Save file", "Save files")]                      // with plural
[Translation("Save file", Context = "Menu")]                  // with context
```

### XAML (WPF)

Declare the Stride presentation namespace and use the `Localize` markup extension:

```xml
xmlns:sd="http://schemas.stride3d.net/xaml/presentation"
```

```xml
<!-- positional (Text is the constructor argument) -->
<Button Content="{sd:Localize Refresh}" />

<!-- named properties -->
<Button Content="{sd:Localize Text=Refresh, Context=Button}" />

<!-- plural (Count is a binding to the count value) -->
<TextBlock Text="{sd:Localize One item, Plural={}{0} items, Count={Binding Count}}" />
```

### XAML (Avalonia)

Same namespace declaration. Use `LocalizeString` for inline strings and `Localize` as a value converter for plural/format patterns:

```xml
xmlns:sd="http://schemas.stride3d.net/xaml/presentation"
```

```xml
<!-- inline string -->
<TextBlock Text="{sd:LocalizeString Add component}" />
<Button ToolTip.Tip="{sd:LocalizeString Refresh, Context=Button}" />

<!-- plural via value converter (bind the count) -->
<Run Text="{Binding Count, Converter={sd:Localize Text={}{0} item, Plural={}{0} items, IsStringFormat=True}}" />
```

> `{}` at the start of a value is XAML's escape sequence for a literal `{` character.

### XAML namespace detection

The extractor identifies `Localize`/`LocalizeString` by the XML namespace URI — not by the prefix. Any prefix works:

```xml
xmlns:loc="clr-namespace:Stride.Core.Translation.Presentation.Wpf.MarkupExtensions;assembly=Stride.Core.Translation.Presentation.Wpf"
xmlns:loc="using:Stride.Core.Presentation.Avalonia.MarkupExtensions"
```

## Translation domains

| Domain | Source directory |
|--------|-----------------|
| `Stride.Core.Presentation` | `sources/presentation/Stride.Core.Presentation` |
| `Stride.Assets.Presentation` | `sources/editor/Stride.Assets.Presentation.Wpf` |
| `Stride.Core.Assets.Editor` | `sources/editor/Stride.Core.Assets.Editor.Wpf` |
| `Stride.GameStudio` | `sources/editor/Stride.GameStudio` |

## Working with .po files

`.po` files are standard gettext catalogues. Any PO editor works (e.g. [Poedit](https://poedit.net/)).

To add a new language, create a new subfolder (e.g. `pl/`) and copy an existing `.po` file as a starting point. `extract_strings.bat` will pick it up on the next run if you add the language code to the `msgmerge` loop at the bottom of the script.

For more context on Stride's localization workflow see the [Localization Guide](https://doc.stride3d.net/latest/en/contributors/engine/localization.html) in the official docs.

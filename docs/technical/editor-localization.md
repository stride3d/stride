# Editor localization

## Introduction

### Rationale
Currently (Stride 2.1) the editor is mostly available in English, although there is very partial support for Japanese. Ideally Stride should be available in a range of languages, starting with English and Japanese. Other languages will probably be be easy to add later if needed.

Supporting multiple languages not only covers every text or tooltip that appear in the UI of the editor, but also error messages, logs and the documentation (we have plan to integrate part of the documentation directly in the Game Studio).

We want also to simplify the workflow of translating the application so that future updates and fixes can be easily integrated. The translation itself will probably be done by an external contractor that doesn't necessarily have technical knowledge of Stride.

And finally, we should have a solution that is flexible enough so that translations can be added/updated without recompiling. This could allow third-party or community-based translation for languages that we won't officially support but that might add value for some people.

### Goals
To summarize, the design goals of the localization system are:

* Easy for developers to add, change or remove text that should be localized.
* Easy for translators to understand the context of the text to be localized, so they can provide the best translation.
  * This also means that they should be provided with a unified document format independent of the underlying technology used.
* Support for versioning.
  * i.e. text format.
* No need to recompile the GameStudio to update a language, an application restart should be able to pickup the latest version.
  * Consider delivery of translation updates outside of the normal release cycle.

### Scope
The localization system should at term support all those cases:

* static UI (mostly XAML)
  * essentially text, but that may also include images or icons
* messages defined in code
  * error messages, logs, etc.
* property grid attributes and types
  * `[Display]`, `[Category]`, enums, types, etc.
* assets
  * game templates that contain a description

### Current state (October 2017)
* [x] static UI
* [x] messages in code (but not all translated)
* [ ] property grid
  * [x] support for enum tooltips
* [ ] assets

## Workflow

### Basic workflow
First localized text need to be identified and "marked". Then they can be extracted to an independent text file (template) which is given to translators. For each supported language translators create or update a file matching the template. Those translated text file can then be imported by the Game Studio and used to display texts and messages in the UI.

In short, development -> extraction -> translation -> import.

A special care should be taken in case of update of existing (and possibly already translated) strings.

### Development
To ease the work of the developer, the API should be minimal and text to translate should be extractable by a tool.

#### XAML
Traditionally, when working with resource files (**.resx**) and satellite assemblies, the developer must lookup the correct key or create a new one if necessary. This is both time-consuming and error-prone.

The current solution, based on a gettext-like technology, use a markup extension (`LocalizeExtension`) for most cases and a value converter (`Translate`) for more advanced cases. The main advantage of a gettext approach is that localization context and plurals are supported out of the box without much trouble. Plurals especially can be complex as the rules differ depending on the language: Japanese doesn't have plurals, Latin-based languages usually have two forms: singular and plural, Arabic has 6 forms, etc.

Examples of use of `LocalizeExtension`:
```xml
<!-- inline -->
<TextBlock Text="{sskk:Localize Hello World!}" />
<!-- string format -->
<TextBlock Text="{Binding Height, StringFormat={sskk:Localize H: {0}}}" />
<!-- plural -->
<TextBlock Text="{sskk:Localize {}{0} item, Plural={}{0} items, Count={Binding ItemCount, Mode=OneWay}, IsStringFormat=True}" />
```

Examples of use of `Translate`:
```xml
<!-- bound to a property -->
<TextBlock Text="{Binding HelloWorld, Converter={sskk:Translate}}" />
<!-- bound to a static reference -->
<TextBlock Text="{Binding Converter={sskk:Translate}, Source={x:Static local:Strings.HelloWorld}}" />
```

Notes:
* Since the converter is used for bindings, the text to localize cannot be extracted from the XAML (same case as when using a static string reference with the markup extension). So the developer must ensure that the related entries are available.
* Plurals and context are not supported as binding (context is supported as a literal attribute), but could be added if necessary using a kind of multi-binding (not implemented yet). A typical use could look like:
```xml
<TextBlock Text="{sskk:MultiTranslate Context={Binding MyContext}, Text={Binding MyText}, PluralText={Binding MyPlural}, Count={Binding MyCount}}" />
```
* Also one difference between the markup extension (Localize) and the converter (Translate) is that the latter converts dynamically. So if the value changes, it will look for the translation of that new value. On the other hand the markup extension works statically: the value is only provided once.

#### C# Code
The main entry point is the `ITranslationManager` interface which is accessed through the singleton `TranslationManager.Instance` (lazy initialized). It is agnostic of the underlying technology (though inspired by Gettext) used for providing the translation and define a minimal interface. Several providers can be registered to the manager (typically one per localized assembly). Through the provider interface (`ITranslationProvider`), developer can query for translated text. For convenience `ITranslationManager` itself implements the `ITranslationProvider` interface.

Initialization (typically in the `Module` class of an assembly):
```csharp
TranslationManager.Instance.RegisterProvider(new GettextTranslationProvider());
```

Change current language:
```csharp
TranslationManager.Instance.CurrentLanguage = new CultureInfo("en-US");
```

Examples of use:
```csharp
// Get a simple string
var str = TranslationManager.Instance.GetString("Some text.");
// Get a string supporting plurals
var plural = TranslationManager.Instance.GetPluralString("{0} fox", "{0} foxes", 42);
// Get a string with a context
var context = TranslationManager.Instance.GetParticularString("Some text.", "some context");
// Get a string with a context and supporting plurals
var contextPlural = TranslationManager.Instance.GetParticularPluralString("{0} fox", "{0} foxes", 42, "some other context");
```

Notes:
* When a translation is not available in the current language, the methods from the provider return the original string.

Like the `ResourceSet` and `ResourceManager` (**.resx** files) that it mimics, Gettext supports language inheritance, i.e. if the current locale is "fr-FR" it will first look for translation for "fr-FR", then fallback to "fr" if not found, then fallback to neutral if not found (then fallback to returning the string as-is as a last resort).

```csharp
TranslationManager.Instance.RegisterProvider(new GettextTranslationProvider());
// Change current culture to French (neutral)
TranslationManager.Instance.CurrentLanguage = new CultureInfo("fr");
// if a French translation exists, returns it. Otherwise returns the same text
Console.WriteLine(provider.GetString("Hello, World!"));
// Change current culture to French (France)
TranslationManager.Instance.CurrentLanguage = new CultureInfo("fr-FR");
// if a French (France) translation exists, returns it. Otherwise looks for French (neutral). Otherwise returns the same text
Console.WriteLine(provider.GetString("Hello, World!"));
```

To localize C# constructs such as class, enum or property, the `TranslationAttribute` can be used. Typical use of this feature includes decorating static strings and enums.

Declarations:
```csharp
public static class Strings
{
    [Translation("Some text")]
    public static readonly string SomeText = "Some text";
}

public enum Groups
{
    [Translation("First group")]
    Group0,
    [Translation("Second group")]
    Group1,
}
```

Uses (C#):
```csharp
var group = Groups.Group0;
Console.WriteLine(TranslationManager.Instance.GetString(group.ToString()));
```

Uses (XAML):
```xml
<TextBlock Text="{Binding Converter={sskk:Translate}, Source={x:Static local:Strings.SomeText}}" />
<TextBlock Text="{Binding Group, Converter={sskk:Translate}}" />
```

### Extraction
Instead of manually creating the resources files, a tool is responsible to extract all localizable strings from the source code (both **.cs** and **.xaml** files).

Notes:
* For convenience, a batch script (**extract_strings.bat**) is provided in **sources\localization**.

#### Export formats
The extractor too can export the strings into a gettext-compatible format (**.pot**). Other formats could be added later (e.g. CSV, XLIFF).

#### Example of exported file
```t
msgid ""
msgstr ""
"Project-Id-Version: \n"
"POT-Creation-Date: 2017-05-26 14:41:20+0900\n"
"PO-Revision-Date: 2017-05-26 14:41:20+0900\n"
"Last-Translator: \n"
"Language-Team: \n"
"MIME-Version: 1.0\n"
"Content-Type: text/plain; charset=utf-8\n"
"Content-Transfer-Encoding: 8bit\n"
"X-Generator: MonoDevelop Gettext addin\n"
 
#: ViewModels/MainWindowViewModel.cs:17
msgid "Some translated text"
msgstr ""
 
#: ViewModels/MainWindowViewModel.cs:14
msgctxt "UI"
msgid "Main Window"
msgstr ""
 
#: ViewModels/MainWindowViewModel.cs:55
msgid "{0} fox"
msgid_plural "{0} foxes"
msgstr[0] ""
msgstr[1] ""
```

Each entry consists of a few elements:
* `msgid` is the original (untranslated) `text`. It corresponds to the text parameter in the `ITranslationProvider` methods.
* `msgid_plural` is the original (untranslated) plural version of the text and is an optional parameter. It corresponds to the `textPlural` parameter in the `ITranslationProvider` methods.
* `msgstr` will be the language-specific translation(s). In a template (.pot) file, it is empty. It will be filled by the translator when creating the dedicated **.po** file for a given language.
  * when a plural form is expected, this become an indexed array of translations. The .pot contains two indexed entries (0 and 1), since it is the default in most Latin-based language (e.g English).
* `msgctxt` is the context of the text and is an optional parameter. It corresponds to the context parameter in the `ITranslationProvider` methods.
* Comments are supported and indicated with a starting **#** character. The character immediately following indicates the type of comment:
  * a whitespace indicates a manual comment, usually added by the translator.
  * a colon (**:**) indicates a source file reference where occurrences of the text appear. If there is more than one occurrence, multiple comments can be used.
  * a point (**.**) indicates a comment added by the developers.
  * other types are supported. See https://www.gnu.org/software/gettext/manual/html_node/PO-Files.html#PO-Files.

#### Merging
The `Catalog` class in the `GNU.Gettext` library already supports some kind of merging with an existing **.pot** file. Additionally, some tools (such as Poedit, see below) support updating an existing translation with a template (**.pot**) file.

When the `--merge` option is used, the existing file is read and new entries found by the extractor tool are added. For the moment, existing entries that are not found again are not deleted but this could be added as an option.

Additionally, the standard distribution of Gettext includes a set of utilities that can be used to automatize the manipulation of existing .po files. This includes:

* ``msgmerge`` to merge two existing .po files together, or to update a **.po** files from a more recent **.pot** files (see https://www.gnu.org/software/gettext/manual/html_node/msgmerge-Invocation.html#msgmerge-Invocation)    * Note that the "merge with POT" option in Poedit is probably based on this utility.
* all kind of manipulations such as comparing, appending, filtering. See complete list here: https://www.gnu.org/software/gettext/manual/html_node/Manipulating.html#Manipulating

##### Text added
New text entries are added to the newly extracted **.pot** template compared to the one that was used to create the existing **.po** files.

In that case, the merge is easy and non conflicting: after using msgmerge (or Poedit), the new entries will be added to the **.po** files with empty translations.

previous **MyApp.fr.po**:
```t
msgid "{0} horse"
msgid_plural "{0} horses"
msgstr[0] "{0} cheval"
msgstr[1] "{0} chevaux"
```

new **MyApp.pot**:
```t
msgid "{0} horse"
msgid_plural "{0} horses"
msgstr[0] ""
msgstr[1] ""
 
msgid "{0} fox"
msgid_plural "{0} foxes"
msgstr[0] ""
msgstr[1] ""
```

new (merged) **MyApp.fr.po**:
```t
msgid "{0} horse"
msgid_plural "{0} horses"
msgstr[0] "{0} cheval"
msgstr[1] "{0} chevaux"
 
msgid "{0} fox"
msgid_plural "{0} foxes"
msgstr[0] ""
msgstr[1] ""
```

##### Text removed
Text entries are missing in the newly extracted **.pot** template compared to the one that was used to create the existing **.po** files.

When using `msgmerge` (or Poedit), the missing entries that have already been translated will be mark as obsolete in the **.po** files (commented out with `#~`). They won't appear in Poedit UI anymore, until they are restored in the **.pot** later. Missing entries that were not translated yet are completely removed to keep the file clean.

previous **MyApp.fr.po**:
```t
msgid "{0} horse"
msgid_plural "{0} horses"
msgstr[0] "{0} cheval"
msgstr[1] "{0} chevaux"
 
msgid "{0} fox"
msgid_plural "{0} foxes"
msgstr[0] "renard"
msgstr[1] "renards"
```

new **MyApp.pot**:
```t
msgid "{0} horse"
msgid_plural "{0} horses"
msgstr[0] ""
msgstr[1] ""
```

new (merged) **MyApp.fr.po**:
```t
msgid "{0} horse"
msgid_plural "{0} horses"
msgstr[0] "{0} cheval"
msgstr[1] "{0} chevaux"
 
#~ msgid "{0} fox"
#~ msgid_plural "{0} foxes"
#~ msgstr[0] "renard"
#~ msgstr[1] "renards"
```

##### Text changed
When an original text in an entry has changed, `msgmerge` (or Poedit) will attempt to find and match the previous text and will mark the translation as fuzzy (i.e. need work). When used with the `--previous` option (which seem to be the case in Poedit), the previous matched text will be preserved (commented out with `#|`).

previous **MyApp.fr.po**:
```t
msgid "{0} horse"
msgid_plural "{0} horses"
msgstr[0] "{0} cheval"
msgstr[1] "{0} chevaux"
```

new **MyApp.pot**:
```t
msgid "{0} big horse"
msgid_plural "{0} big horses"
msgstr[0] ""
msgstr[1] ""
```

new (merged) **MyApp.fr.po**:
```t
#, fuzzy
#| msgid "{0} horse"
#| msgid_plural "{0} horses"
msgid "{0} big horse"
msgid_plural "{0} big horses"
msgstr[0] "{0} cheval"
msgstr[1] "{0} chevaux"
```
Note that if `msgmerge` (or Poedit) cannot find and match a previous text, it will fallback to the add/remove cases.

Of course, after the merge the new file must be transmitted to the translators for update.

### Translation
After having extracted the localizable strings, the next step is to actually translate them into the supported languages.

The **.pot** file is a textual format that can theoretically be edited manually. However, this can be a difficult especially for non-technical people. Fortunately a number of tools exist than can recognize this format and work with it smoothly.

#### Using Poedit
Poedit ([website](https://poedit.net/), [source code (MIT)](https://github.com/vslavik/poedit/)) allows to create and manage gettext catalog file (**.po**). It also support creating and updating an existing catalog from a template (**.pot**) applying the correct plural rules corresponding to the selected language.

Poedit application is translated into several language including Japanese (100% according to the [Crowdin project](https://crowdin.com/project/poedit/ja)).

##### Open a file (**.pot** or **.po**)
Poedit supports opening both **.po** (catalog) and **.pot** (template) files. The main difference is that a template is not bound to a particular localization while a catalog is. Templates contains extracted strings and ce be seen as a read-only input in the translation process. Catalogs can be modified and are the output of this process.

After opening a template file, the translator can create a new translation by clicking on the "Create new translation" button at the bottom. It opens a dialog where the target language can be selected. This is important in order to setup the correct plural rules for the chosen language.

![](media/poedit-open-pot-file.png)

##### Editing a catalog
The process of editing in poedit is straightforward. A list of source strings is visible at the top. After selecting an entry, the translation (or translations incase of plurals) can be edited at the bottom. Poedit also offers translation suggestions based on some external database, but is not always accurate and is a limited feature in the free version.

Comments (added by developers and by the extraction tool) are also visible at the bottom right. Translators can also add their own comments. This can be use for example to communicate between the translators team and the developers.

![](media/poedit-edit-po-file.png)

It also supports a "Need work" flag (that is translated into the fuzzy flag in the **.po** file) to indicate when a translation might be incorrect or need more work. In that case it will appear in orange in the list. The flag is usually set by the extraction tool, but translators can also set or unset it at will.

##### Update from a more recent template
After a translation has been created by a translator, changes can still happen as development continues. To deal with this case, Poedit has an option to update a catalog (**.po**) from a template (**.pot**). To do so go to **Menu-->Catalog-->Update from POT files...** and select the corresponding file.

What happens is that any new entry is added while previously existing one are kept as-is. There is also a very neat option when a source text has slightly changed, Poedit will detect it and display the entry has fuzzy while also indicated that it detected a change.

On the other hand, if an entry is completely removed, it will disappear from the Poedit interface but will stay in the **.po** file (commented out using the `#~` prefix) until it is purged. One advantage of not removing it completely is that if it is reintroduced, the existing translation will be restored.

Notes:
* Internally Poedit seem to be using the `msgmerge` tool (see "Merging" section above).

##### Saving
By convention the name of the catalog file should match the name of the template with the addition of the target language (in [IETF language tag](https://en.wikipedia.org/wiki/IETF_language_tag) format). It should also be saved in a folder corresponding to that language.

For example the Japanese catalog for `Stride.GameStudio` should be named **Stride.GameStudio.ja.po** and saved in a **ja/** folder.

### Import

#### Supported import formats
For the moment we only support **.po** and **.resx** files compiled into satellite assemblies. However the `Stride.Core.Translation` library is flexible and can be extended to support additional providers. This could include CSV (not necessarily compiled), XLIFF, **.mo** files (which is another kind of compiled **.po**). This could also be considered for asset translation (e.g. **.sdtpl**) where we externalize the translations into files that can be distributed separately and loaded/discovered using a dedicated provider.

#### Compilation to satellite assemblies
**.po** files can be compiled into a satellite assemblies that the `GettextTranslationProvider` (and under the hood the `GNU.Gettext` library) will use to retrieve translations for a given language. It it a similar mechanism to the satellite assemblies generated from the **.resx files**. In fact `GettextResourceManager` inherits from `ResourceManager` with additional support for capabilities provided by Gettext such as context and plurals.

To create such assembly, the GNU.Gettext.Msgfmt.exe command line must be used. Usage is show below:

```
Msgfmt (Gettext.NET tools)
Custom message formatter from *.po to satellite assembly DLL or to *.resources files
 
Usage:
    GNU.Gettext.Msgfmt[.exe] [OPTIONS] filename.po ...
   -r resource, --resource=resource    Base name for resources catalog i.e. 'Solution1.App2.Module3'
                                       Note that '.Messages' suffix will be added for using by GettextResourceManager
 
   -o file, --output-file=file         Output file name for .NET resources file.
                                       Ignored when -d is specified
 
   -d directory                        Output directory for satellite assemblies.
                                       Subdirectory for specified locale will be created
 
   -l locale, --locale=locale          .NET locale (culture) name i.e. "en-US", "en" etc.
 
   -L path, --lib-dir=path             Path to directory where GNU.Gettext.dll is located (need to compile DLL)
 
   --compiler-name=name                C# compiler name.
                                       Defaults are "mcs" for Mono and "csc" for Windows.NET.
                                       On Windows you should check if compiler directory is in PATH environment variable
 
   --check-format                      Verify C# format strings and raise error if invalid format is detected
 
   --csharp-resources                  Convert a PO file to a .resources file instead of satellite assembly
 
   -v, --verbose                       Verbose output
 
   -h, --help                          Display this help and exit
```

The command needs to find a C# compiler in the path (in our case csc that can be found in the Roslyn folder under the *MSBuild* installation).

In Stride's projects file, a command line similar to the following one is used:

```
Path=$(MSBuildBinPath)\Roslyn;$(Path)
IF EXIST "$(SolutionDir)..\sources\localization\ja\$(TargetName).ja.po" "$(SolutionDir)..\sources\common\deps\Gettext.Net\GNU.Gettext.Msgfmt.exe" --lib-dir "$(SolutionDir)..\sources\common\deps\Gettext.Net" --resource $(TargetName) -d "$(TargetDir)." --locale ja "$(SolutionDir)..\sources\localization\ja\$(TargetName).ja.po" --verbose
```

Remarks:

* By convention, the **.po** filenames are suffixed by the [IETF language tag](https://en.wikipedia.org/wiki/IETF_language_tag) (e.g. "en" for English, "fr" for French, "ja" for Japanese). Note that the same tags are recognized by the .Net `CultureInfo` class.
* The generated satellite assemblies must be located into a dedicated subfolder relative to where the executable is (same rule as with assemblies generated from **.resx** files). The command line already takes care of it through the `-d` argument.
* The generated satellite assemblies are named after the corresponding assembly that is localized, suffixed by ".Messages" (then by ".resources" as is the convention for satellites). For example for **Stride.GameStudio.exe** the satellite assembly is named **Stride.GameStudio.Messages.resources.dll**

### Update
When developers add or remove the strings that can be localized, the same workflow as described above must be run. Because it includes changes in both the original assemblies and the satellite assemblies, it implies that a new version of the product must be released.

There are some cases though were we might just want to correct typos, without adding or removing localizable strings. Normally developers would fix the typos in code. But that means that they "keys" of the gettext catalog would change which will require updating all translations as well.

If only a few corrections are needed, there is another alternative. The localizable strings present in the code (**.cs** or **.xaml** file) are considered as *neutral* language, not as *English*. So there is a way to provide an English translation to them. While that may sound a bit silly (those strings are already in English), it is a nice hack to make quick fixes.

Consider this: instead of fixing in code, rebuilding the whole product and updating all translations and then releasing the whole as a brand new Stride version, developers just need to follow the same workflow as with any other language but by creating an English translation instead (e.g. **Stride.GameStudio.en.po**). Then just fix the entries that need fixing and generate a satellite assembly (e.g. **Stride.GameStudio.Messages.resources.dll**) and distribute it. Tell the users to copy it into a **en/** folder in **Bin/Windows** of Stride installation and voilà! At runtime when the English language is selected (which is the default), the translation system will pick-up this satellite assembly and uses its entries as a translation for English.

## Implementation details

### `GNU.Gettext` assembly
This assembly contains a port of GNU gettext to .Net. It is part of the [Gettext for .NET/Mono project](https://sourceforge.net/projects/gettextnet/).

#### `GettextResourceManager` class
This class inherits from `System.Resources.ResourceManager`. Instances of this class are used by the `GettextTranslationProvider` to retrieve translations from localized strings. A resource manager handles one or several resource sets corresponding to a language and its derivatives (e.g. "en" and "en-US").

#### `GettextResourceSet` class
This class inherits from `System.Resources.ResourceSet`. Instances of this class will be generated from **.po** files and compiled into satellite assemblies. A resource set is a big hashtable of strings with the original localized string as key and the translated string as value.

### `Stride.Core.Translation` assembly
This assembly contains the translation API that developers will use.

#### `ITranslationProvider` class
Interface defining the API of a translation provider. The methods signatures imitate the API provided by `GettextResourceManager`.

#### `GettextTranslationProvider` class
Basically a wrapper around a `GettextResourceManager`.

#### `ResxTranslationProvider` class
Basically a wrapper around a `GettextResourceManager`, provided as a convenience class in case we need to also include satellite assemblies generated from **.resx** files.

It doesn't support context and plurals (as this feature is only provided by gettext) but returns nicely either a normal translation of the string (i.e. singular and without context) or the string itself. It is preferable to throwing an exception, although it would be better to also log that behavior.

#### `TranslationManager` class
Main entry point of the API, its implementation is hidden and a single instance (singleton) is available through the `TranslationManager.Instance` static property.

Providers can be registered to it (this usually happens in the `Module` class of its localized assembly). When the user requests the translation of a string, the manager will select the correct provider based on the calling assembly name.

The manager itself implements the `ITranslationProvider`. This is convenient and enables scenario where nested manager could be used (register a manager as a provider of another manager).

##### `GetString(string text)`
| Parameter name | Description             |
|----------------|-------------------------|
| text           | The string to translate |

Example:
```csharp
Console.WriteLine(TranslationManager.Instance.GetString("Hello World!"));
```

##### `GetPluralString(string text, string textPlural, int count)`
| Parameter name | Description                                  |
|----------------|----------------------------------------------|
| text           | The string to translate                      |
| pluralText     | The plural version of the text to translate  |
| count          | An integer used to determine the plural form |

Example:
```csharp
long count = 2;
Console.WriteLine(TranslationManager.Instance.GetPluralString("Hello World!", "Hello Worlds!", count));
```

##### `GetParticularString(string context, string text)`
| Parameter name | Description                     |
|----------------|---------------------------------|
| context        | The context for the translation |
| text           | The string to translate         |

Example:
```csharp
Console.WriteLine(TranslationManager.Instance.GetParticularString("Messages", "Hello World!"));
```

##### `GetParticularPluralString(string context, string text, string textPlural, int count)`
| Parameter name | Description                                  |
|----------------|----------------------------------------------|
| context        | The context for the translation              |
| text           | The string to translate                      |
| pluralText     | The plural version of the text to translate  |
| count          | An integer used to determine the plural form |

Example:
```csharp
long count = 2;
Console.WriteLine(TranslationManager.Instance.GetParticularPluralString("Messages", "Hello World!", "Hello Worlds!", count));
```

#### `TranslationAttribute` class
Sometimes, we need to localize certain C# constructs such as enum values, especially when they are displayed to the end-user. For that purpose, the `TranslationAttribute` can be used.

Example:
```csharp
public enum Hoyle
{
    [Translation("Big")]
    Big,
    [Translation("Bang")]
    Bang,
}
```

#### `Tr` helper class
Writing `TranslationManager.Instance.GetString()` for every call to the translation API is a bit long. For that reason convenient shortcuts are provided in the `Tr` helper class. The following table describes the relation between the shortcut method and the corresponding API:

| `Tr`                                | `TranslationManager.Instance`                             |
|-------------------------------------|-----------------------------------------------------------|
| `_(text)`                           | `GetString(text)`                                         |
| `_n(text, textPlural, count)`           | `GetPluralString(text, textPlural, count)`                    |
| `_p(context, text)`                 | `GetParticularString(context, text)`                      |
| `_pn(context, text, textPlural, count)` | `GetParticularPluralString(context, text, textPlural, count)` |

### `Stride.Core.Translation.Presentation` assembly

This assembly enables the support of localization in **.xaml** files.

#### `LocalizeExtension` class

This markup extension has a double use. It is used by the extractor to detect localizable strings. At runtime it uses the localization API to provide the correct string depending on the language.

Examples of use in XAML:
```xml
<!-- inline -->
<TextBlock Text="{sskk:Localize My text}" />

<!-- same, with verbose syntax -->
<TextBlock Text="{sskk:Localize Text=My text}" />

<!-- with a context -->
<TextBlock Text="{sskk:Localize My text, Context=Menu}" />

<!-- with a singular and plural, count defined by a binding -->
<TextBlock Text="{sskk:Localize My text, Plural=My texts, Count={Binding Collection.Count}}" />

<!-- with a formatted singular and plural, count defined by a binding -->
<TextBlock Text="{sskk:Localize {}{0} text, Plural={}{0} texts, Count={Binding Collection.Count}, IsStringFormat=True}" />
```

#### `LocalizeConverter` class
Base class for markup extensions/value converters that support some kind of localization. The base class retrieve the current local assembly where this markup extension/value converter is used. Inheriting classes can then pass this assembly as parameter to the `TranslationManager` methods to get the corresponding translation. Currently three converters inherits from this class: `EnumToTooltip`, `ContentReferenceToUrl` and `Translate`. The first two already existed and were adapted to support localization.

#### `Translate` class
The `LocalizeExtension` described above can only localize static strings and can't be used in bindings. For that scenario the `Translate` markup extension/value converter can be used. It will dynamically query the translation manager with the bound value converted to a `string`.

Note that for the localization to work, the bound value must match one of the localized string.

### `Stride.Core.Translation.Extractor` standalone
The extractor is a standalone command line that can be used to retrieve all *localizable* strings from **.cs** and **.xaml** source file and generate a template **.pot** file.

The usage of the command line is 
```
Stride.Core.Translation.Extractor[.exe] [options] [inputfile | filemask] ...
```

 with the following options: 
* `-D directory` or `--directory=directory`: Look for files in the given directory. This option can be added more than once.
* `-r` or `--recursive`: Look for files in sub-directories.
* `-x` or `--exclude=filemask`: Exclude a file or filemask from the search.
* `-d` or `--domain-name=name`: Output 'name.pot' instead of default 'messages.pot'
* `-b` or `--backup`: Create a backup file (.bak) in case the output file already exists
* `-o file` or `--output=file`: Write output to specified file (instead of 'name.pot or 'messages.pot').
* `-m` or `--merge`: Attempt to merge extracted strings with an existing file.
* `-C` or `--preserve-comments`: Attempt to preserve comments on existing entries.
* `-v` or `--verbose`: More verbose message in the command prompt.
* `-h` or `--help`: Display usage and exit.

For example to extract the strings for the `Stride.GameStudio` project, the command line is:

```
Stride.Core.Translation.Extractor -D ..\editor\Stride.GameStudio -d Stride.GameStudio -r -C -x *.Designer.cs *.xaml *.cs
```

It will look into all **.xaml** and **.cs** files in the whole project (*recursive* option) except the file matching the **\*.Designer.cs** pattern and output the extracted strings into `Stride.GameStudio.pot` (*domain-name* option). Existing comments will be preserved.

Notes:
* Internally it uses the C#-port of the Gettext library, retrieved from the seemingly non-longer maintained [Gettext for .NET/Mono](https://sourceforge.net/projects/gettextnet/) (last update 2016-05-08). Note that the source code is provided under the LGPL v2 license so if we make modifications we need to publish it under the same license. Maybe we should fork it (and publish it on GitHub) to be on the safe side.
* For the moment I didn't have to do any modification as I rewrote the extractor tool, instead of using/modifying the one that cam with the code (GNU.Gettext.Xgettext), so using the compiled binaries of the `GNU.Gettext.dll` (use at design time and runtime) and `GNU.Getopt.dll` (used only at design time) is fine.

#### `CSharpExtractor` class
This class parses **.cs** files and extracts the localizable strings by matching regular expressions with the methods from `ITranslationProvider` interface and `Tr` helper class.

Note: using regular expressions is not perfect and ideally we should parse the **.cs** file properly (using Roslyn?) to make sure we don't get false positives.

#### `XamlExtractor` class
This class parses **.xaml** files and extracts strings that are localized with the `{sskk:Localize}` extension. It uses a `XamlReader` to parse the nodes, which is more robust than regular expressions.

#### `POExporter` class
This class exports the extracted strings in a template **.pot** file that can be then used by translators. It uses the capabilities from the `GNU.Gettext.Catalog` class to manage and save the **.pot** file.

#### `ResxExporter` class
Not yet implemented. The idea is to be able to export the extracted strings in a regular **.resx** file, in case this format is to be used.

## Documentation and references
https://docs.microsoft.com/en-us/aspnet/core/fundamentals/localization IStringLocalizer (Asp.Net Core)

https://docs.microsoft.com/en-us/windows/uwp/globalizing/prepare-your-app-for-localization using resw files

https://en.wikipedia.org/wiki/XLIFF standardized format for localization files. See https://github.com/Microsoft/XLIFF2-Object-Model for a possible implementation.

### Tools and frameworks
Gettext references: https://www.gnu.org/software/gettext/manual/index.html, for C# https://www.gnu.org/software/gettext/manual/html_node/C_0023.html

Alternative implementation of gettext for .Net (https://github.com/neris/NGettext)

Another .Net implementation (https://sourceforge.net/projects/gettextnet/)

Could be combined with https://github.com/pdfforge/translatable

#### Tools
ResX editor: https://github.com/UweKeim/ZetaResourceEditor

Poedit (for gettext .po files): https://poedit.net/. Source code here: https://github.com/vslavik/poedit/

Babylon.Net: http://www.redpin.eu/

Pootle: http://pootle.translatehouse.org/. Source code here: https://github.com/translate/pootle

### Misc.
http://wp12674741.server-he.de/tiki/tiki-index.php

http://www.tbs-apps.com/lsacreator/

https://crowdin.net/ crowd-source localization (nice community, but only for the texts, still need a tool to collect and a tool to build)

https://weblate.org/en/ free web-based translation software. The company also offers hosting plan and support for a price, but self hosting is possible.



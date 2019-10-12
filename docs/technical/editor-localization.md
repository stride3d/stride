# 编辑器本地化

## 介绍

### 基本原理
目前（Xenko 3.0），该编辑器主要以英语提供，尽管对日语的支持非常有限。理想情况下，Xenko应该以多种语言提供，从英语和日语开始。如果需要，以后可能会很容易添加其他语言。

支持多种语言不仅涵盖编辑器UI中出现的每个文本或工具提示，还涵盖错误消息，日志和文档（我们计划将部分文档直接集成到Game Studio中）。

我们还希望简化翻译应用程序的工作流程，以便将来的更新和修补程序可以轻松集成。翻译本身可能由不一定具有Xenko技术知识的外部承包商完成。

最后，我们应该有一个足够灵活的解决方案，以便无需重新编译就可以添加/更新翻译。这可能允许第三方或基于社区的语言翻译我们不正式支持的语言，但可能会为某些人增加价值。

### 目标
总而言之，本地化系统的设计目标是：

* 开发人员可以轻松添加，更改或删除应本地化的文本。
* 便于翻译人员理解要本地化的文本的上下文，因此他们可以提供最佳的翻译。
  * 这也意味着应为他们提供统一的文档格式，而与所使用的基础技术无关。
* 支持版本控制。
  * 即文本格式。
* 无需重新编译GameStudio即可更新语言，重新启动应用程序应能够获取最新版本。
  * 考虑在正常发布周期之外交付翻译更新。

### 范围
本地化系统应在所有情况下支持以下情况：

* 静态用户界面（主要是XAML）
  * 本质上是文本，但其中也可能包含图片或图标
* 代码中定义的消息
  * 错误消息，日志等
* 属性网格的属性和类型
  * `[Display]`，`[Category]`，枚举，类型等。
* 资源
  * 包含说明的游戏模板

### 当前状态（2017年10月）
* [x]静态用户界面
* 代码中的[x]条消息（但不是全部翻译）
* []属性格
  * [x]支持枚举工具提示
* []资源

## 工作流程

### 基本工作流程
首先需要标识本地化文本并对其进行“标记`。然后可以将它们提取到一个独立的文本文件（模板）中，该文件将提供给翻译人员。对于每种受支持的语言，翻译人员都会创建或更新与模板匹配的文件。这些翻译的文本文件随后可以由Game Studio导入，并用于在UI中显示文本和消息。

简而言之，开发 ->提取 ->翻译 ->导入。

在更新现有（可能已经翻译）字符串的情况下，应格外小心。

### 开发
为了简化开发人员的工作，API应该尽量少，并且要翻译的文本应该可以通过工具提取。

#### XAML
传统上，当使用资源文件（**.resx**）和附属程序集时，开发人员必须查找正确的密钥或在必要时创建新的密钥。这既费时又容易出错。

当前的解决方案基于类似gettext的技术，在大多数情况下使用标记扩展（`LocalizeExtension`），在更高级的情况下使用值转换器（`Translate`）。 gettext方法的主要优点是开箱即用地支持本地化上下文和复数形式，而不会带来太多麻烦。复数形式尤其复杂，因为规则因语言而异：日语没有复数形式，基于拉丁语的语言通常具有两种形式：单数和复数形式，阿拉伯语有6种形式，依此类推。

使用`LocalizeExtension`的示例：
```xml
<!-- 内联 -->
<TextBlock Text="{sskk:Localize Hello World!}" />
<!-- 字符串格式 -->
<TextBlock Text="{Binding Height, StringFormat={sskk:Localize H: {0}}}" />
<!-- 复数 -->
<TextBlock Text="{sskk:Localize {}{0} item, Plural={}{0} items, Count={Binding ItemCount, Mode=OneWay}, IsStringFormat=True}" />
```

使用`Translate`的示例：
```xml
<!-- 绑定到属性 -->
<TextBlock Text="{Binding HelloWorld, Converter={sskk:Translate}}" />
<!-- 绑定到静态引用 -->
<TextBlock Text="{Binding Converter={sskk:Translate}, Source={x:Static local:Strings.HelloWorld}}" />
```

笔记：
* 由于转换器用于绑定，因此无法从XAML提取要本地化的文本（与使用带有标记扩展名的静态字符串引用时相同）。因此，开发人员必须确保相关条目可用。
* 不支持将复数形式和上下文作为绑定（将上下文作为文字属性来支持），但是可以根据需要使用一种多重绑定（尚未实现）来添加。典型用法如下：
```xml
<TextBlock Text="{sskk:MultiTranslate Context={Binding MyContext}, Text={Binding MyText}, PluralText={Binding MyPlural}, Count={Binding MyCount}}" />
```
* 标记扩展（Localize）和转换器（Translate）之间的另一个区别是后者是动态转换的。因此，如果值更改，它将寻找该新值的转换。另一方面，标记扩展是静态工作的：该值仅提供一次。

#### C#代码
主要入口是`ITranslationManager`接口，可通过单例`TranslationManager.Instance`（延迟初始化）进行访问。它与用于提供翻译并定义最小接口的基础技术（尽管受Gettext启发）无关。可以向管理者注册多个提供者（通常每个本地程序集一个）。通过提供者接口（`ITranslationProvider`），开发人员可以查询翻译后的文本。为了方便起见，ITranslationManager本身实现了ITranslationProvider接口。

初始化（通常在程序集的`Module`类中）：
```csharp
TranslationManager.Instance.RegisterProvider(new GettextTranslationProvider());
```

更改当前语言：
```csharp
TranslationManager.Instance.CurrentLanguage = new CultureInfo("en-US");
```

使用示例：
```csharp
//获取一个简单的字符串
var str = TranslationManager.Instance.GetString("Some text.");
//获取支持复数的字符串
var plural = TranslationManager.Instance.GetPluralString("{0} fox", "{0} foxes", 42);
//获取带有上下文的字符串
var context = TranslationManager.Instance.GetParticularString("Some text.", "some context");
//获取带有上下文并支持复数的字符串
var contextPlural = TranslationManager.Instance.GetParticularPluralString("{0} fox", "{0} foxes", 42, "some other context");
```

笔记：
* 当当前语言无法提供翻译时，提供者的方法将返回原始字符串。

就像它模仿的`ResourceSet`和`ResourceManager`（**.resx**文件）一样，Gettext支持语言继承，即如果当前语言环境为`fr-FR`，则它将首先查找`fr-FR`的翻译。 如果未找到，则回退为`fr`，如果未找到，则回退为默认（然后回退为按原样返回字符串）。

```csharp
TranslationManager.Instance.RegisterProvider(new GettextTranslationProvider());
//将当前文化更改为法语（中性）
TranslationManager.Instance.CurrentLanguage = new CultureInfo("fr");
//如果存在法语翻译，则将其返回。否则返回相同的文本
Console.WriteLine(provider.GetString("Hello, World!"));
//将当前文化更改为法语（法国）
TranslationManager.Instance.CurrentLanguage = new CultureInfo("fr-FR");
//如果存在法语（法国）翻译，则将其返回。否则寻找法语（中性）。否则返回相同的文本
Console.WriteLine(provider.GetString("Hello, World!"));
```

为了本地化C#构造，例如类，枚举或属性，可以使用TranslationAttribute。此功能的典型用法包括装饰静态字符串和枚举。

声明：
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

使用（C#）：
```csharp
var group = Groups.Group0;
Console.WriteLine(TranslationManager.Instance.GetString(group.ToString()));
```

使用（XAML）：
```xml
<TextBlock Text="{Binding Converter={sskk:Translate}, Source={x:Static local:Strings.SomeText}}" />
<TextBlock Text="{Binding Group, Converter={sskk:Translate}}" />
```

### 提取
无需手动创建资源文件，而是使用一种工具负责从源代码（**.cs**和**.xaml**文件）中提取所有可本地化的字符串。

笔记：
* 为方便起见，在**sources\localization**中提供了一个批处理脚本（**extract_strings.bat**）。

#### 导出格式
提取器也可以将字符串导出为与gettext兼容的格式（**.pot**）。以后可以添加其他格式（例如CSV，XLIFF）。

#### 导出文件的示例
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

每个条目都包含一些元素：
* `msgid`是原始的（未翻译的）`text`。它对应于`ITranslationProvider`方法中的text参数。
* `msgid_plural`是文本的原始（未翻译）复数版本，并且是一个可选参数。它对应于ITranslationProvider方法中的textPlural参数。
* `msgstr`将是特定语言的翻译。在模板（.pot）文件中，该文件为空。当为给定语言创建专用的**.po**文件时，它将由翻译人员填充。
  * 当期望复数形式时，它将成为翻译的索引数组。 .pot包含两个索引条目（0和1），因为它是大多数基于拉丁语的语言（例如英语）的默认设置。
* `msgctxt`是文本的上下文，是可选参数。它对应于`ITranslationProvider`方法中的上下文参数。
* 支持注释，并以**#**开头字符表示。紧随其后的字符表示注释的类型：
  * 空格表示手动注释，通常由翻译人员添加。
  * 冒号（**:**）表示在其中出现文本的源文件引用。如果出现多个，则可以使用多个注释。
  * 点（**.**）表示开发人员添加的评论。
  * 支持其他类型。请参阅 https://www.gnu.org/software/gettext/manual/html_node/PO-Files.html#PO-Files。

### 合并
GNU.Gettext`库中的`Catalog`类已经支持某种与现有**.pot**文件的合并。此外，某些工具（例如Poedit，请参见下文）支持使用模板（**.pot**）文件更新现有翻译。

使用--merge选项时，将读取现有文件，并添加提取器工具找到的新条目。目前，不会再次找到的现有条目不会被删除，但这可以作为一个选项添加。

此外，Gettext的标准发行版包括一组实用程序，可用于自动处理现有.po文件。这包括：

* ``msgmerge``将两个现有的.po文件合并在一起，或从较新的**.pot**文件更新**.po**文件（请参阅 https://www.gnu.org/software/gettext/manual/html_node/msgmerge-Invocation.html#msgmerge-Invocation）* 请注意，Poedit中的“与POT合并`选项可能基于此实用程序。
* 各种操作，例如比较，附加，过滤。请在此处查看完整列表：https://www.gnu.org/software/gettext/manual/html_node/Manipulating.html#Manipulating

##### 添加文字
与用于创建现有**.po**文件的模板相比，新的文本条目将添加到新提取的**.pot**模板中。

在这种情况下，合并很容易且没有冲突：使用msgmerge（或Poedit）后，新条目将以空翻译添加到**.po**文件中。

以前的**MyApp.fr.po**：
```t
msgid "{0} horse"
msgid_plural "{0} horses"
msgstr[0] "{0} cheval"
msgstr[1] "{0} chevaux"
```

新的**MyApp.pot**：
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

新（合并）**MyApp.fr.po**：
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

### 删除的文字
与用于创建现有**.po**文件的模板相比，新提取的**.pot**模板中缺少文本条目。

当使用`msgmerge`（或Poedit）时，已经翻译的缺失条目将在**.po**文件中被标记为过时（用##注释）。它们将不会再出现在Poedit UI中，直到稍后在**.pot**中将它们还原。尚未翻译的缺失条目将被完全删除，以保持文件干净。

以前的**MyApp.fr.po**：
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

新的**MyApp.pot**：
```t
msgid "{0} horse"
msgid_plural "{0} horses"
msgstr[0] ""
msgstr[1] ""
```

新（合并）**MyApp.fr.po**：
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

### 文字已更改
当条目中的原始文本更改时，`msgmerge`（或Poedit）将尝试查找并匹配先前的文本，并将翻译标记为模糊（即需要工作）。与`--previous`选项一起使用时（在Poedit中似乎是这种情况），先前匹配的文本将被保留（用##注释）。

以前的**MyApp.fr.po**：
```t
msgid "{0} horse"
msgid_plural "{0} horses"
msgstr[0] "{0} cheval"
msgstr[1] "{0} chevaux"
```

新的**MyApp.pot**：
```t
msgid "{0} big horse"
msgid_plural "{0} big horses"
msgstr[0] ""
msgstr[1] ""
```

新（合并）**MyApp.fr.po**：
```t
#, fuzzy
#| msgid "{0} horse"
#| msgid_plural "{0} horses"
msgid "{0} big horse"
msgid_plural "{0} big horses"
msgstr[0] "{0} cheval"
msgstr[1] "{0} chevaux"
```
请注意，如果`msgmerge`（或Poedit）无法找到和匹配先前的文本，它将回退到添加/删除案例。

当然，合并后，必须将新文件传输到翻译器以进行更新。

### 翻译
提取可本地化的字符串后，下一步是将它们实际翻译成支持的语言。

**.pot**文件是一种文本格式，理论上可以手动进行编辑。但是，这对于非技术人员尤其困难。幸运的是，目前有许多工具可以识别这种格式并可以平稳地使用它。

#### 使用Poedit
Poedit（[网站(https://poedit.net)，[源代码（MIT）](https://github.com/vslavik/poedit/)）允许创建和管理gettext目录文件（**.po**）。它还支持使用与所选语言相对应的正确复数规则从模板（**.pot**）创建和更新现有目录。

Poedit应用程序已翻译成多种语言，包括日语（根据[Crowdin项目](https://crowdin.com/project/poedit/ja)的100％）。

##### 打开文件（**.pot**或**.po**）
Poedit支持打开**.po**（目录）和**.pot**（模板）文件。主要区别在于，模板没有绑定到特定的本地目录。模板包含提取的字符串，并且在翻译过程中可以被视为只读输入。目录可以修改，并且是此过程的输出。

打开模板文件后，翻译人员可以通过单击底部的“创建新翻译`按钮来创建新翻译。它会打开一个对话框，可以在其中选择目标语言。为了为所选语言设置正确的复数规则，这一点很重要。

![](media/poedit-open-pot-file.png)

##### 编辑目录
poedit中的编辑过程非常简单。源字符串列表在顶部可见。选择条目后，可以在底部编辑译文（或复数形式的译文）。 Poedit还提供了基于某些外部数据库的翻译建议，但并不总是准确的，并且在免费版本中功能有限。

注释（由开发人员和提取工具添加）也显示在右下角。译者还可以添加自己的评论。例如，这可以用于在翻译团队和开发人员之间进行沟通。

![](media/poedit-edit-po-file.png)

它还支持“需要工作`标志（已转换为**.po**文件中的模糊标志），以指示何时翻译不正确或需要更多工作。在这种情况下，它将在列表中以橙色显示。该标志通常由提取工具设置，但翻译人员也可以随意设置或取消设置它。

##### 从较新的模板更新
译员创建翻译后，随着开发的继续，变更仍然可能发生。为了处理这种情况，Poedit可以选择从模板（**.pot**）更新目录（**.po**）。为此，请转到菜单 -->目录 -->从POT文件更新...**，然后选择相应的文件。

发生的情况是，添加了任何新条目，而以前存在的条目保持原样。当源文本稍有更改时，还有一个非常简洁的选项，Poedit将检测到它并显示条目模糊，同时还指示它检测到更改。

另一方面，如果一个条目被完全删除，它将从Poedit界面中消失，但是将保留在**.po**文件中（使用`#~`前缀注释），直到将其清除。不完全删除它的一个好处是，如果重新引入它，则会还原现有的翻译。

笔记：
* 内部Poedit似乎正在使用`msgmerge`工具（请参见上面的"合并"部分）。

##### 保存
按照约定，目录文件的名称应与模板名称和目标语言相匹配（[IETF语言标记](https://en.wikipedia.org/wiki/IETF_language_tag)格式）。也应将其保存在与该语言对应的文件夹中。

例如，`Xenko.GameStudio`的日语目录应命名为`Xenko.GameStudio.ja.po**`，并保存在`ja/**`文件夹中。

### 导入

#### 支持的导入格式
目前，我们仅支持编译到附属程序集中的**.po**和**.resx**文件。但是，`Xenko.Core.Translation`库是灵活的，可以扩展以支持其他提供程序。这可能包括CSV（不一定是已编译），XLIFF，**.mo**文件（这是另一种已编译的**.po**）。对于资源翻译（例如**.xktpl**），我们也可以考虑使用此方法，其中我们将翻译外部化为文件，这些文件可以单独分发，也可以使用专用提供程序加载/发现。

#### 编译为附属程序集
可以将**.po**文件编译为附属程序集，`GettextTranslationProvider`（以及在其内部的`GNU.Gettext`库）将用于检索给定语言的翻译。它与从**.resx文件**生成的附属程序集的机制类似。实际上，`GettextResourceManager`继承自`ResourceManager`，并且对Gettext提供的功能（如上下文和复数）有额外的支持。

要创建这样的程序集，必须使用GNU.Gettext.Msgfmt.exe命令行。用法如下所示：

```
Msgfmt（Gettext.NET工具）
从*.po到附属程序集DLL或*.resources文件的自定义消息格式器
 
用法：
    GNU.Gettext.Msgfmt[.exe] [OPTIONS] filename.po ...
   -r resource, --resource=resource    资源目录的基本名称，即`Solution1.App2.Module3`
                                       请注意，将添加`.Messages`后缀以供GettextResourceManager使用
 
   -o file, --output-file=file         .NET资源文件的输出文件名。
                                       当指定-d时被忽略
 
   -d directory                        附属程序集的输出目录。
                                       将创建指定语言环境的子目录
 
   -l locale, --locale=locale          .NET语言环境（区域性）名称，即`en-US`，`en`等。
 
   -L path, --lib-dir=path             指向GNU.Gettext.dll所在目录的路径（需要编译DLL）

   --compiler-name=name                C#编译器名称。
                                       对于Mono，默认值为`mcs`；对于Windows.NET，默认值为`csc`。
                                       在Windows上，您应该检查编译器目录是否在PATH环境变量中
 
   --check-format                      验证C#格式字符串并在检测到无效格式时引发错误
 
   --csharp-resources                  将PO文件转换为.resources文件，而不是附属程序集
 
   -v, --verbose                       详细输出
 
   -h, --help                          显示此帮助并退出
```

该命令需要在路径中找到一个C#编译器（在本例中为csc，可在* MSBuild * 安装下的Roslyn文件夹中找到）。

在Xenko的项目文件中，使用类似于以下内容的命令行：

```
Path=$(MSBuildBinPath)\Roslyn;$(Path)
IF EXIST "$(SolutionDir)..\sources\localization\ja\$(TargetName).ja.po" $(SolutionDir)..\sources\common\deps\Gettext.Net\GNU.Gettext.Msgfmt.exe --lib-dir $(SolutionDir)..\sources\common\deps\Gettext.Net --resource $(TargetName) -d $(TargetDir) --locale ja "$(SolutionDir)..\sources\localization\ja\$(TargetName).ja.po" --verbose
```

备注：

* 按照惯例，**.po**文件名以[IETF语言标签](https://en.wikipedia.org/wiki/IETF_language_tag)为后缀（例如，`en`代表英语，`fr`代表法语，日语为`ja`）。注意，.Net`CultureInfo`类可以识别相同的标签。
* 生成的附属程序集必须位于相对于可执行文件所在的专用子文件夹中（与从**.resx**文件生成的程序集相同的规则）。命令行已经通过-d参数处理了它。
* 生成的附属程序集以本地化的相应程序集命名，后缀以`.Messages`（后缀为`.resources`，这是集合的惯例）。例如，对于**Xenko.GameStudio.exe**，附属程序集名为**Xenko.GameStudio.Messages.resources.dll**

### 更新
当开发人员添加或删除可以本地化的字符串时，必须运行与上述相同的工作流程。因为它同时包含原始装配和附属装配中的更改，所以它意味着必须发布该产品的新版本。

在某些情况下，我们可能只是想纠正拼写错误，而不添加或删除可本地化的字符串。通常，开发人员会在代码中修正拼写错误。但这意味着gettext目录的“键`将更改，这也需要更新所有翻译。

如果只需要进行一些更正，则还有另一种选择。代码（**.cs**或**.xaml**文件）中存在的可本地化的字符串被视为*中性* 语言，而不是*英语* 。因此，有一种方法可以为他们提供英文翻译。尽管这听起来有些愚蠢（这些字符串已经是英文的），但是它是进行快速修复的不错的工具。

考虑一下这一点：开发人员无需固定代码，重建整个产品并更新所有翻译，然后将其发布为全新的Xenko版本，开发人员只需遵循与任何其他语言相同的工作流程，而只需创建英文翻译即可（例如**Xenko.GameStudio.zh.po**）。然后只需修复需要修复的条目并生成附属程序集（例如**Xenko.GameStudio.Messages.resources.dll**）并分发即可。告诉用户将其复制到Xenko安装和版本的**Bin / Windows**中的**en /**文件夹中！在运行时，选择英语（默认设置）时，翻译系统将提取该附属程序集并将其条目用作英语翻译。

## 实施细节

### `GNU.Gettext`程序集
该程序集包含一个到.Net的GNU gettext端口。它是[用于.NET/Mono项目的Gettext](https://sourceforge.net/projects/gettextnet/)的一部分。

### `GettextResourceManager`类
此类从`System.Resources.ResourceManager`继承。该类的实例被GettextTranslationProvider使用，以从本地化字符串中检索翻译。资源管理器处理对应于一种语言及其派生词的一个或几个资源集（例如`en`和`en-US`）。

### `GettextResourceSet`类
此类从`System.Resources.ResourceSet`继承。此类的实例将从**.po**文件生成，并编译为附属程序集。资源集是一个很大的字符串哈希表，原始的本地化字符串为键，转换后的字符串为值。

### `Xenko.Core.Translation`程序集
该程序集包含开发人员将使用的翻译API。

### `ITranslationProvider`类
定义翻译提供程序的API的接口。方法签名模仿了由GettextResourceManager提供的API。

### `GettextTranslationProvider`类
基本上是一个围绕GetTextResourceManager的包装器。

### `ResxTranslationProvider`类
基本上是围绕`GettextResourceManager`的包装器，作为方便类提供，以防万一我们还需要包含从**.resx**文件生成的附属程序集。

它不支持上下文和复数形式（因为此功能仅由gettext提供），但可以很好地返回字符串的正常转换（即单数形式，没有上下文）或字符串本身。最好抛出一个异常，尽管也最好记录该行为。

### `TranslationManager`类
API的主要入口点，其实现是隐藏的，并且可以通过`TranslationManager.Instance`静态属性使用单个实例（单例）。

提供者可以注册到它（这通常发生在其本地化程序集的`Module`类中）。当用户请求翻译字符串时，管理器将基于调用程序集名称选择正确的提供程序。

管理器本身实现了`ITranslationProvider`。这很方便，并且启用了可以使用嵌套管理器的方案（将一个管理器注册为另一个管理器的提供者）。

##### `GetString(string text)`
|参数名称|描述|
|----------------|-------------------------|
|文字             |要翻译的字符串			   |

例：
```csharp
Console.WriteLine(TranslationManager.Instance.GetString("Hello World!"));
```

##### `GetPluralString(string text, string textPlural, int count)`
| Parameter name | Description                                  |
|参数名称|描述|
|----------------|----------------------------------------------|
|文字|要翻译的字符串|
|复数文字|翻译的文字的复数形式|
|计数|用于确定复数形式的整数|

例：
```csharp
long count = 2;
Console.WriteLine(TranslationManager.Instance.GetPluralString("Hello World!", "Hello Worlds!", count));
```

##### `GetParticularString(string context, string text)`
|参数名称|描述|
|----------------|----------------------------------------------|
|上下文|翻译的上下文|
|文字|要翻译的字符串|

例：
```csharp
Console.WriteLine(TranslationManager.Instance.GetParticularString("Messages", "Hello World!"));
```

##### `GetParticularPluralString(string context, string text, string textPlural, int count)`
|参数名称|描述|
|----------------|----------------------------------------------|
|上下文|翻译的上下文|
|文字|要翻译的字符串|
|复数文字|翻译的文字的复数形式|
|计数|用于确定复数形式的整数|

例：
```csharp
long count = 2;
Console.WriteLine(TranslationManager.Instance.GetParticularPluralString("Messages", "Hello World!", "Hello Worlds!", count));
```

### `TranslationAttribute`类
有时，我们需要对某些C#构造（例如枚举值）进行本地化，尤其是当它们显示给最终用户时。为此，可以使用TranslationAttribute。

例：
```csharp
public enum Hoyle
{
    [Translation("Big")]
    Big,
    [Translation("Bang")]
    Bang,
}
```

### `Tr`助手类
为每次对翻译API的调用编写`TranslationManager.Instance.GetString()`有点长。因此，在`Tr`帮助器类中提供了方便的快捷方式。下表描述了快捷方式方法和相应的API之间的关系：

| `Tr`                                | `TranslationManager.Instance`                             |
|-------------------------------------|-----------------------------------------------------------|
| `_(text)`                           | `GetString(text)`                                         |
| `_n(text, textPlural, count)`           | `GetPluralString(text, textPlural, count)`                    |
| `_p(context, text)`                 | `GetParticularString(context, text)`                      |
| `_pn(context, text, textPlural, count)` | `GetParticularPluralString(context, text, textPlural, count)` |

### `Xenko.Core.Translation.Presentation`程序集

该程序集支持对**.xaml**文件中的本地化的支持。

#### `LocalizeExtension`类

此标记扩展名有双重用途。提取程序使用它来检测可本地化的字符串。在运行时，它会根据语言使用本地化API提供正确的字符串。

在XAML中使用的示例：
```xml
<!-- 内联 -->
<TextBlock Text="{sskk:Localize My text}" />

<!-- 同样，具有冗长的语法 -->
<TextBlock Text="{sskk:Localize Text=My text}" />

<!-- 与上下文 -->
<TextBlock Text="{sskk:Localize My text, Context=Menu}" />

<!-- 具有单数和复数，计数由绑定定义 -->
<TextBlock Text="{sskk:Localize My text, Plural=My texts, Count={Binding Collection.Count}}" />

<!-- 带有格式的单数和复数，计数由绑定定义 -->
<TextBlock Text="{sskk:Localize {}{0} text, Plural={}{0} texts, Count={Binding Collection.Count}, IsStringFormat=True}" />
```

### `LocalizeConverter`类
支持某种本地化的标记扩展/值转换器的基类。基类检索使用此标记扩展/值转换器的当前本地程序集。然后，继承类可以将该程序集作为参数传递给`TranslationManager`方法以获取相应的翻译。 Currently three converters inherits from this class: `EnumToTooltip`, `ContentReferenceToUrl` and `Translate`. The first two already existed and were adapted to support localization.

#### `Translate`类
上述的`LocalizeExtension`只能本地化静态字符串，不能在绑定中使用。对于这种情况，可以使用“翻译”标记扩展/值转换器。它将动态查询翻译管理器，并将绑定值转换为`string`。

请注意，要使本地化工作，绑定值必须与本地化字符串之一匹配。

###`Xenko.Core.Translation.Extractor`独立
提取程序是一个独立的命令行，可用于从**.cs**和**.xaml**源文件中检索所有*可本地化的字符串，并生成模板*localizable*文件。

命令行的用法是
```
Xenko.Core.Translation.Extractor[.exe] [options] [inputfile | filemask] ...
```

 具有以下选项：
*`-D directory`或`--directory= directory`：在给定目录中查找文件。可以多次添加此选项。
*`-r`或`--recursive`：在子目录中查找文件。
*`-x`或`--exclude= filemask`：从搜索中排除文件或文件掩码。
*`-d`或`--domain-name=name`：输出'name.pot'而不是默认的'messages.pot'
* -b或--backup：在输出文件已经存在的情况下创建备份文件（.bak）
*`-o file`或`--output=file`：将输出写入指定的文件（而不是'name.pot或'messages.pot'）。
*`-m`或`--merge`：尝试将提取的字符串与现有文件合并。
* -C或--preserve-comments：尝试保留现有条目的注释。
*`-v`或`--verbose`：命令提示符中的更多详细信息。
*`-h`或`--help`：显示用法并退出。

例如，要提取Xenko.GameStudio项目的字符串，命令行是：

```
Xenko.Core.Translation.Extractor -D ..\editor\Xenko.GameStudio -d Xenko.GameStudio -r -C -x *.Designer.cs *.xaml *.cs
```

它将查找整个项目中的所有**。xaml **和**。cs **文件（*递归*选项），除了匹配** \ * .Designer.cs **模式的​​文件之外，并输出提取的字符串进入`Xenko.GameStudio.pot`（*域名*选项）。现有评论将被保留。

笔记：
* 在内部，它使用Gettext库的C＃端口，该C＃端口从似乎不再保留的[.NET / Mono Gettext](https://sourceforge.net/projects/gettextnet/)中检索（最新更新2016-05-05 08）。请注意，源代码是根据LGPL v2许可证提供的，因此，如果我们进行修改，则需要以相同的许可证发布。为了安全起见，也许我们应该分叉它（并在GitHub上发布）。
* 目前，当我重新编写提取器工具时，无需进行任何修改，而不必使用/修改带有代码（GNU.Gettext.Xgettext）的工具，因此可以使用`GNU的已编译二进制文件。 Gettext.dll（在设计时和运行时使用）和GNU.Getopt.dll（仅在设计时使用）很好。

#### `CSharpExtractor`类
此类解析**.cs**文件并通过将正则表达式与ITranslationProvider接口和Tr 帮助类中的方法匹配来提取可本地化的字符串。

注意：使用正则表达式并不完美，理想情况下，我们应该正确解析**.cs**文件（using Roslyn?），以确保不会出现误报。

####`XamlExtractor`类
此类解析**.xaml**文件并提取以{sskk:Localize}扩展名本地化的字符串。它使用XamlReader解析节点，这比正则表达式更强大。

#### `POExporter`类
此类将提取的字符串导出到**.pot**模板文件中，然后供翻译人员使用。它使用`GNU.Gettext.Catalog`类的功能来管理和保存**.pot**文件。

#### `ResxExporter`类
尚未实现。这样的想法是能够导出提取的字符串到常规**.resx**文件中，以防使用这种格式。

## 文档和参考
https://docs.microsoft.com/zh-cn/aspnet/core/fundamentals/localization IStringLocalizer (Asp.Net Core)

使用resw文件的 https://docs.microsoft.com/zh-cn/windows/uwp/globalizing/prepare-your-app-for-localization

本地化文件的 https://en.wikipedia.org/wiki/XLIFF 标准化格式。有关可能的实现，请参见 https://github.com/Microsoft/XLIFF2-Object-Model。

### 工具和框架
Gettext参考：https://www.gnu.org/software/gettext/manual/index.html ，用于C＃ https://www.gnu.org/software/gettext/manual/html_node/C_0023.html

.Net的gettext的替代实现（https://github.com/neris/NGettext）

另一个.Net实现（https://sourceforge.net/projects/gettextnet/）

可以与 https://github.com/pdfforge/translatable 结合使用
 
#### 工具
ResX编辑器：https://github.com/UweKeim/ZetaResourceEditor

Poedit（用于gettext.po文件）：https://poedit.net/ 。源代码在这里：https://github.com/vslavik/poedit/

Babylon.Net: http://www.redpin.eu/

Pootle：http：//pootle.translatehouse.org/。源代码在这里：https://github.com/translate/pootle

### 其他
http://wp12674741.server-he.de/tiki/tiki-index.php

http://www.tbs-apps.com/lsacreator/

https://crowdin.net/ 群众资源本地化（很好的社区，但仅针对文本，仍需要收集工具和构建工具）

https://weblate.org/en/ 基于网络的免费翻译软件。 该公司还提供托管计划和价格支持，但可以进行自助托管。



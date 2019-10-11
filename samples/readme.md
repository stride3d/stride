# Xenko范例

- 每个样本必须是使用**GameStudio创建的自包含Xenko游戏包**
	- 这意味着样本包不得在其目录之外引用资产/文件
- 示例程序包必须使用唯一的程序包名称，并且可以在使用该文件的文件中用简单的正则表达式替换（.csproj，.cs ...等）。例如：`SimpleAudio`
- 我们目前使用以下类别作为目录，将同一类别下的样本分组
	- `Audio`：所有与音频有关的样本
	- `Games`：所有小游戏样本
	- `Graphics`：所有图形样本（显示3D模型，子画面，文本等）
	- `Input`：所有输入示例（触摸，鼠标，游戏板等）
	- `UI`：所有UI示例
	- `XenkoSamples.sln`：引用所有游戏包（xkpkg）的解决方案`XenkoSamples.sln`
- 在类别内，我们将包存储在其自己的目录中。例如`Audio`中的`SimpleAudio`
	- `Audio`
		- `SimpleAudio`
		- `.xktpl`：包含用于在UI中显示模板的图标/屏幕截图的目录
		- `Assets`：包含资产（.xk文件）
		- `Resources`：包含资源文件（.jpg，.fbx ...文件）
		- `SimpleAudio.Android`：Android可执行文件
		- `SimpleAudio.Game`：常见游戏代码
		- `SimpleAudio.iOS`：iOS可执行文件
		- `SimpleAudio.Windows`：Windows桌面可执行文件
		- `SimpleAudio.xkpkg`：软件包描述
		- `SimpleAudio.xktpl`：包模板描述



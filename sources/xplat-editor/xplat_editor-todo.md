# Xplat Editor - TODO

## MVP

### Basic editor layout

- [x] Fixed layout (e.g. Grid)
- [ ] Docks (postponed)

### Open a Stride Project

- [x] Have a memory representation
	- [x] Use the MSBuild API? (latest .NET 6 is Microsoft.Build 17.3.2)?
	- [x] Use the functionalities from Stride.Core.Assets
- [ ] Have the explorer (read-only) in the UI

### Build a Stride project

- [ ] Call dotnet API
	- Use the MSBuild API?
	- Start a process and call the command-line?
- [ ] Display log results in the UI

### Open a scene

- [ ] Tree explorer
	- no editing
	- no property grid
	
### Visualize a scene (rendering)
- [ ] Scene rendering
	- embedded Stride scene
	- no interaction (default camera position and direction?)

## Useful links

- [Add Linux support for the Stride Game Studio #1922](https://github.com/stride3d/stride/issues/1922)
- [[RFC] Migrate to AvaloniaUI #1629](https://github.com/stride3d/stride/issues/1629)
- [Stride UI discussion](https://gist.github.com/Eideren/6424455fd25f3820bbce6594d67e307b)
- [Stride UI discussion (list)](https://gist.github.com/Eideren/4eb0199e87eb0a89092a3cd21332aa47)
- [Stride Editor current design document](https://gist.github.com/manio143/b6666eedb1403deb5525961697d0c25d)
- [StrideComponentsEditorAvalonia](https://github.com/Kryptos-FR/StrideComponentsEditorAvalonia)
- [Stridelonia](https://github.com/TheKeyblader/Stridelonia)

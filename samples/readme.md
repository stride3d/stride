# Xenko Samples

- Each sample must be a **self-contained Xenko Game Package, created with GameStudio**
	- It means that a sample package must not reference assets/files outside its directory
- A sample package must use a package name that is unique and can be replaced by a simple regex in files using it (.csproj, .cs ...etc.). For example: `SimpleAudio`
- We are currently using the following categories as directories to group samples under a same category 
	- `Audio` : All samples related to audio
	- `Games` : All small game samples
	- `Graphics` : All graphics samples (display 3d models, sprites, text...etc.)
	- `Input` : All input samples (touch, mouse, gamepad...etc.)
	- `UI` : All UI samples
	- `XenkoSamples.sln` : A top level `XenkoSamples.sln` referencing all Game Packages (xkpkg)
- Inside a category, we store a package in its own directory. For example `SimpleAudio` in `Audio`
	- Audio
		- `SimpleAudio`
			- `.xktpl` : Directory containing icons/screenshots used to display the template in the UI
			- `Assets` : contains assets (.xk files)
			- `Resources` : contains resource files (.jpg, .fbx ... files)
			- `SimpleAudio.Android` : Android executable
			- `SimpleAudio.Game` : Common Game code
			- `SimpleAudio.iOS` : iOS executable
			- `SimpleAudio.Windows` : Windows Desktop executable
			- `SimpleAudio.xkpkg` : Package description
			- `SimpleAudio.xktpl` : Package Template description






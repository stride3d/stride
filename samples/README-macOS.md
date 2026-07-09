# Running Samples on macOS

Use `run-macos-sample.sh` from the repository root:

```sh
./samples/run-macos-sample.sh list
./samples/run-macos-sample.sh ParticlesSample
./samples/run-macos-sample.sh AnimatedModel
```

The runner uses Vulkan through MoltenVK. With Apple Silicon Homebrew defaults, it
sets these automatically when present:

```sh
SDL_VULKAN_LIBRARY=/opt/homebrew/lib/libvulkan.1.dylib
VK_DRIVER_FILES=/opt/homebrew/etc/vulkan/icd.d/MoltenVK_icd.json
VK_ICD_FILENAMES=/opt/homebrew/etc/vulkan/icd.d/MoltenVK_icd.json
```

Useful options:

```sh
BUILD_ONLY=true ./samples/run-macos-sample.sh SimpleAudio
SKIP_BUILD=true ./samples/run-macos-sample.sh SimpleAudio
SHOW_BUILD_WARNINGS=true ./samples/run-macos-sample.sh SimpleAudio
```

Checked-in `.macOS` launcher projects are used directly. For Windows-only samples,
the runner creates a temporary launcher under `samples/.generated-macos-launchers/`
and keeps the sample output under the sample's normal `Bin/macOS/<Configuration>/`
folder.

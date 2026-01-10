# SDK Status Command

Check the status of the WIP SDK-style build system rework.

## Usage

```
/sdk-status
```

## Instructions

The `sources/sdk/` directory contains the work-in-progress SDK-style build system rework. This is the current focus of the `feature/stride-sdk` branch.

### Check Current Status

1. List all files in the SDK directory:
```
sources/sdk/
```

2. Read the key SDK files:
   - `Stride.Sdk/Sdk.props` - SDK properties
   - `Stride.Sdk/Sdk.targets` - SDK targets
   - Any README or documentation files

3. Compare with existing targets:
   - `sources/targets/` - Current MSBuild props/targets

### Key Questions to Answer

- What SDK packages are being created?
- What target frameworks are supported?
- What build customizations are included?
- How does it differ from the current system in `sources/targets/`?
- What's left to implement?

### Related Files

- `sources/Directory.Build.props` - Root build properties
- `sources/Directory.Build.targets` - Root build targets
- `sources/targets/Stride.props` - Main Stride properties
- `sources/targets/Stride.targets` - Main Stride targets
- `build/Stride.build` - Advanced build targets

Report the current state of the SDK work, what's implemented, and what remains to be done.

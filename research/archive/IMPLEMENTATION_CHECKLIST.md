# Implementation Checklist & Deployment Guide

## Overview

This checklist ensures successful integration of the Stride Native Build Enhancement across the repository.

---

## Pre-Implementation Checklist

### Prerequisites Verification
- [ ] LLVM tools available in `deps/LLVM/` (clang.exe, lld.exe present)
- [ ] .NET SDK 6.0+ installed for all developers
- [ ] MSBuild capable of processing XML correctly
- [ ] Windows environment has long file paths enabled (`fsutil`)
- [ ] CI/CD agents can download LLVM deps

### Documentation Review
- [ ] All team members read `QUICKSTART_AND_SUMMARY.md`
- [ ] Build engineers review `BUILD_ARCHITECTURE_ANALYSIS.md`
- [ ] CI/CD team reviewed `INTEGRATION_AND_MIGRATION_GUIDE.md`
- [ ] QA team has `TECHNICAL_COMPARISON_MSVC_VS_LLD.md`

### Backup & Rollback Plan
- [ ] Create branch `feature/native-clang-lld`
- [ ] Tag current stable: `pre-clang-lld-migration`
- [ ] Document MSVC fallback procedure
- [ ] Have MSVC build available as backup

---

## Integration Phase (7 Minutes)

### Step 1: Add Build Mode Configuration
- [ ] File `sources/targets/Stride.NativeBuildMode.props` exists
- [ ] File copied to correct location
- [ ] Content verified (XML is valid)
- [ ] Test: `msbuild /t:StrideNativeBuildModeHelp` target callable

### Step 2: Add LLD Targets File
- [ ] File `sources/native/Stride.Native.Windows.Lld.targets` exists
- [ ] File copied to correct location
- [ ] Content verified (XML is valid)
- [ ] All platform targets present (x86, x64, ARM64)

### Step 3: Update Stride.Core.targets
- [ ] Found import line for `Stride.Native.targets` (~line 139)
- [ ] Added `Stride.NativeBuildMode.props` import before it
- [ ] Verified XML syntax is correct
- [ ] No duplicate imports

### Step 4: Update Stride.Native.targets
- [ ] Found end of file (before closing `</Project>`)
- [ ] Added `Stride.Native.Windows.Lld.targets` import
- [ ] Verified XML syntax
- [ ] Tested: `msbuild /t:StrideNativeBuildModeHelp` works

### Step 5: (Optional) Update MSVC Target Condition
- [ ] Found `CompileNativeClang_Windows` target (~line 155)
- [ ] Updated condition to include build mode check
- [ ] Verified XML syntax
- [ ] Condition restricts MSVC path to legacy mode

---

## Compilation Verification (Step-by-Step)

### Local Developer Test

```bash
# 1. Clean previous builds
dotnet clean sources/engine/Stride.Audio/Stride.Audio.csproj

# 2. Test 1: Default build (should use Clang+LLD)
dotnet build sources/engine/Stride.Audio/Stride.Audio.csproj /v:normal

# Expected output:
# - [Stride] Native build mode: Clang
# - [Stride] Clang compiling C/C++ files
# - [Stride] Linked libstrideaudio.dll for Windows x64
# - Build succeeded
```

✅ **Verification**: Check output directory
```bash
dir sources\engine\Stride.Audio\bin\Debug\net*\runtimes\win-x64\native\
# Should show: libstrideaudio.dll
```

```bash
# 2. Test 2: Backward compatibility (MSVC mode)
dotnet build sources/engine/Stride.Audio/Stride.Audio.csproj /p:StrideNativeBuildMode=Msvc /v:normal

# Expected output:
# - [Stride] Native build mode: Msvc
# - Calls WindowsDesktop.vcxproj (if available)
# - If vcxproj missing: error message with helpful fallback
```

```bash
# 3. Test 3: Incremental build
dotnet build sources/engine/Stride.Audio/Stride.Audio.csproj

# Expected: Skips native compilation (sources unchanged)
```

### CI/CD Pipeline Test

```yaml
# test-clang-lld.yml
name: Test Clang+LLD Build

on: [push, pull_request]

jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v2
      - uses: actions/setup-dotnet@v1
        with:
          dotnet-version: '6.0.x'
      
      - name: Build with Clang+LLD
        env:
          StrideNativeBuildMode: Clang
        run: dotnet build sources/engine/Stride.Audio/Stride.Audio.csproj
      
      - name: Verify DLL
        run: |
          if (Test-Path "sources/engine/Stride.Audio/bin/Debug/net6.0/runtimes/win-x64/native/libstrideaudio.dll") {
            Write-Host "Success: DLL built"
          } else {
            Write-Error "Failed: DLL not found"
          }
```

---

## Testing Phase

### Functional Tests

#### Test 1: Load Native Library from C#
```csharp
[DllImport("libstrideaudio", CallingConvention = CallingConvention.Cdecl)]
private static extern bool InitializeAudio();

[Test]
public void TestNativeLibraryLoad()
{
    // This should not throw
    bool result = InitializeAudio();
    Assert.IsNotNull(result);
}
```

- [ ] Test passes with Clang-built DLL
- [ ] Test passes with MSVC-built DLL
- [ ] Test passes in Release mode
- [ ] Test passes on different architectures

#### Test 2: Cross-Platform Consistency
- [ ] Build on Windows x86: DLL created ✓
- [ ] Build on Windows x64: DLL created ✓
- [ ] Build on Windows ARM64 (if available): DLL created ✓
- [ ] Output files identical size/signature

#### Test 3: Performance Baseline
```powershell
# Measure build time
Measure-Command {
    dotnet clean
    dotnet build /p:StrideNativeBuildMode=Clang
} | Select TotalSeconds

# Compare with MSVC
Measure-Command {
    dotnet clean
    dotnet build /p:StrideNativeBuildMode=Msvc
} | Select TotalSeconds
```

- [ ] Record baseline (MSVC)
- [ ] Record Clang+LLD time
- [ ] Calculate difference
- [ ] Document if regression > 10%

#### Test 4: Error Handling
```bash
# Test 1: Missing LLVM
mv deps\LLVM deps\LLVM.bak
dotnet build 2>&1 | grep "Clang not found"
mv deps\LLVM.bak deps\LLVM

# Test 2: Broken C++ code
# Intentionally introduce syntax error in Native/*.cpp
dotnet build 2>&1 | grep "error"
# Should show clang error, not linker error
```

- [ ] Clear error messages shown
- [ ] Helpful suggestions provided
- [ ] Errors are actionable

#### Test 5: Incremental Build
```bash
# First build
time dotnet build

# Second build (no changes)
time dotnet build
# Should be significantly faster

# Touch one C++ file
touch sources/engine/Stride.Audio/Native/Celt.cpp

# Third build
time dotnet build
# Should rebuild only Celt.cpp, not all files
```

- [ ] First build completes
- [ ] Second build is fast (skips native)
- [ ] Partial rebuild works correctly

---

## Deployment Checklist

### Code Review
- [ ] Stride.Core.targets changes reviewed
- [ ] Stride.Native.targets changes reviewed
- [ ] Stride.NativeBuildMode.props reviewed
- [ ] Stride.Native.Windows.Lld.targets reviewed
- [ ] No unintended changes to other files

### Documentation
- [ ] README.md updated with new approach
- [ ] Developer guide updated
- [ ] Build instructions updated
- [ ] Troubleshooting guide created
- [ ] CI/CD template updated
- [ ] Inline comments added to new targets

### Team Notification
- [ ] Team lead notified of changes
- [ ] Build engineers briefed
- [ ] QA team informed
- [ ] Documentation team briefed
- [ ] Release notes prepared

### Branch Management
- [ ] Feature branch created: `feature/native-clang-lld`
- [ ] All changes committed
- [ ] Pull request created
- [ ] CI/CD pipeline passes
- [ ] Code review approved
- [ ] Merged to main development branch

---

## Post-Deployment Monitoring (Week 1)

### Daily Checks
- [ ] Monitor CI/CD pipeline builds
- [ ] Check for linker errors or warnings
- [ ] Review build time metrics
- [ ] Track reported issues

### Weekly Checks
- [ ] Analyze performance data
- [ ] Review error logs
- [ ] Gather developer feedback
- [ ] Prepare adjustments if needed

### Metrics to Track
```
- Build success rate: Target > 99%
- Build time: Baseline ± 10%
- Native DLL size: Baseline ± 5%
- Error count: Target = 0
- Developer complaints: Track and resolve
```

- [ ] Dashboard created
- [ ] Metrics collected daily
- [ ] Reports generated weekly
- [ ] Anomalies investigated

---

## Rollback Procedure (If Needed)

### Immediate Rollback (Emergency)
```bash
# Use MSVC mode as fallback
export StrideNativeBuildMode=Msvc
dotnet build
```

- [ ] Environment variable set on all build agents
- [ ] Developers notified
- [ ] Builds continue with MSVC

### Repository Rollback
```bash
# If critical issues found in implementation files
git revert <commit-hash>
# or
git checkout pre-clang-lld-migration -- sources/native sources/targets
```

- [ ] Previous tag available
- [ ] Rollback procedure documented
- [ ] Team trained on rollback

### Analysis Phase
- [ ] Collect error logs
- [ ] Identify root cause
- [ ] Plan fix
- [ ] Retry deployment

---

## Success Criteria

### Must Have (Release Blocking)
- [ ] All projects build successfully
- [ ] Native DLLs are functionally identical
- [ ] No performance regression > 20%
- [ ] Backward compatibility (MSVC mode works)
- [ ] CI/CD pipelines pass

### Should Have (High Priority)
- [ ] Performance improves > 5%
- [ ] Build time consistent
- [ ] Error messages clear
- [ ] Documentation complete
- [ ] Zero breaking changes

### Nice to Have (Future Phases)
- [ ] Cross-compilation working
- [ ] PDB debug info generated
- [ ] Platform-specific optimizations
- [ ] Custom build tool integration

**Status**: All must-have items must be ✅ before release

---

## Sign-Off Sheet

### For Release Manager
```
Reviewed all changes:      ☐
Verified testing complete:  ☐
Documentation updated:      ☐
CI/CD pipeline green:       ☐
Date approved:              ________
```

### For Build Engineers
```
Local testing passed:       ☐
CI/CD integration tested:   ☐
Rollback procedure verified: ☐
Date verified:              ________
```

### For QA
```
Functional tests passed:    ☐
Performance tests complete: ☐
Error handling verified:    ☐
Date signed off:            ________
```

### For Team Lead
```
Team briefed:               ☐
Risks understood:           ☐
Benefits communicated:      ☐
Deployment approved:        ☐
Date approved:              ________
```

---

## Timeline

### Week 1: Integration
- Mon: File integration (7 min)
- Tue: Local testing
- Wed: CI/CD testing
- Thu: Documentation review
- Fri: Team sign-off

### Week 2: Deployment
- Mon-Tue: Soft launch (beta)
- Wed: Full deployment
- Thu-Fri: Monitoring and adjustments

### Week 3+: Stabilization
- Monitor for issues
- Gather feedback
- Plan optimizations
- Prepare deprecation plan for MSVC mode

---

## Related Documentation

| Document | Purpose | Audience |
|----------|---------|----------|
| QUICKSTART_AND_SUMMARY.md | Overview & quick reference | Everyone |
| BUILD_ARCHITECTURE_ANALYSIS.md | Technical deep-dive | Build engineers |
| INTEGRATION_AND_MIGRATION_GUIDE.md | Step-by-step integration | Build engineers, QA |
| TECHNICAL_COMPARISON_MSVC_VS_LLD.md | Performance comparison | QA, managers |
| NATIVE_BUILD_IMPLEMENTATION_GUIDE.md | Advanced configuration | Advanced developers |

---

## Questions & Support

### Common Questions

**Q: Will this break my existing builds?**  
A: No, existing projects work unchanged. Default mode is Clang+LLD, MSVC mode available via flag.

**Q: Do I need to install Visual Studio?**  
A: No, LLVM in deps/ is sufficient. Visual Studio remains optional.

**Q: What if I find an issue?**  
A: Set `StrideNativeBuildMode=Msvc` as temporary workaround, report issue with details.

**Q: How do I opt-out of the new system?**  
A: Set `StrideNativeBuildMode=Msvc` via environment variable or MSBuild property.

### Support Contacts
- Build System: [Team Lead Name]
- Native Development: [C++ Lead Name]
- CI/CD Pipeline: [DevOps Name]
- Documentation: [Docs Lead Name]

---

## Final Verification

Before declaring deployment complete:

```
✅ All checklist items completed
✅ Tests passed (all categories)
✅ Documentation updated
✅ Team trained
✅ Metrics baseline established
✅ Rollback procedure verified
✅ No open issues blocking deployment
✅ Stakeholder sign-offs received
```

**Deployment Status**: Ready for production

---

**Last Updated**: January 31, 2026  
**Version**: 1.0  
**Status**: Ready for implementation

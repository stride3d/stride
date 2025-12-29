# Stride SDK Modernization Roadmap

**Date:** December 28, 2025  
**Purpose:** Roadmap for creating Stride.Sdk to modernize and simplify the build configuration

## Executive Summary

This roadmap outlines the plan to create a custom MSBuild SDK (`Stride.Sdk`) that will:
- Simplify project files across the Stride engine
- Improve compatibility with modern .NET tooling
- Centralize build logic in a versioned NuGet package
- Enable better IDE support and IntelliSense
- Align with modern .NET SDK patterns

## Current State Analysis

### Existing Build Structure

**Current Import Chain:**
```
Project.csproj
  └─> sources/targets/Stride.props (for engine projects)
      └─> sources/targets/Stride.Core.props
          ├─> Stride.Core.TargetFrameworks.Editor.props
          ├─> build/Stride.Build.props (if exists)
          ├─> build/Stride.Core.Build.props (if exists)
          └─> Sdk.props (Microsoft.NET.Sdk) - imported at END
          
  └─> sources/targets/Stride.targets (implicit, at end)
      └─> sources/targets/Stride.Core.targets
          ├─> build/Stride.Build.targets (if exists)
          └─> build/Stride.Core.Build.targets (if exists)
```

### Current Project File Structure

**Typical Stride project:**
```xml
<Project>
  <PropertyGroup>
    <StrideRuntime>true</StrideRuntime>
  </PropertyGroup>

  <Import Project="..\..\targets\Stride.Core.props" />

  <PropertyGroup>
    <ProductVersion>8.0.30703</ProductVersion>
    <SchemaVersion>2.0</SchemaVersion>
    <StrideAssemblyProcessor>true</StrideAssemblyProcessor>
    <StrideBuildTags>*</StrideBuildTags>
    <!-- ... many more properties ... -->
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\OtherProject\OtherProject.csproj" />
  </ItemGroup>

  <Import Project="$(StrideSdkTargets)" />
</Project>
```

### Problems with Current Approach

1. **Tool Incompatibility:** Projects without SDK attribute aren't recognized by many tools
2. **Verbose Project Files:** Lots of boilerplate in every project
3. **Complex Import Chains:** Hard to understand evaluation order
4. **Manual Maintenance:** Updates require touching many project files
5. **Non-Standard:** Doesn't follow modern .NET conventions

### Key Custom Features to Preserve

1. **Multi-Platform Support:**
   - StrideFramework (net10.0)
   - StrideFrameworkWindows (net10.0-windows)
   - StrideFrameworkAndroid (net10.0-android)
   - StrideFrameworkiOS (net10.0-ios)
   - StrideFrameworkUWP (uap10.0.16299)

2. **Graphics API Multi-Targeting:**
   - Direct3D11, Direct3D12, OpenGL, OpenGLES, Vulkan
   - StrideGraphicsApiDependent builds
   - Platform-specific API defaults

3. **Platform Detection:**
   - StridePlatform (Windows, Linux, macOS, Android, iOS, UWP)
   - StridePlatformDeps
   - Platform-specific defines

4. **Assembly Processing:**
   - StrideAssemblyProcessor
   - Custom serialization and module initialization

5. **UI Framework Selection:**
   - SDL, WinForms, WPF support
   - StrideUI property

## Proposed Architecture

### Target SDK Project Structure

```
sources/sdk/Stride.Sdk/
├── Stride.Sdk.csproj              # SDK package project
├── Sdk/
│   ├── Sdk.props                  # Main properties file
│   ├── Sdk.targets                # Main targets file
│   ├── Stride.Graphics.props      # Graphics API specific logic
│   ├── Stride.Platforms.props     # Platform detection
│   ├── Stride.Runtime.props       # Runtime multi-targeting
│   └── Stride.AssemblyProcessor.targets
├── build/
│   ├── Stride.Sdk.props           # Legacy NuGet support
│   └── Stride.Sdk.targets         # Legacy NuGet support
├── tools/
│   └── (future: custom MSBuild tasks)
└── README.md
```

### Proposed Project File Structure

**Future Stride project:**
```xml
<Project Sdk="Stride.Sdk">
  <PropertyGroup>
    <StrideRuntime>true</StrideRuntime>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\OtherProject\OtherProject.csproj" />
  </ItemGroup>
</Project>
```

**Reduction:** ~50-70% less code per project file

### global.json Configuration

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestMinor"
  },
  "msbuild-sdks": {
    "Stride.Sdk": "4.3.0"
  }
}
```

## Implementation Phases

## Phase 1: Analysis & Planning ✅ COMPLETE

**Duration:** 2 weeks  
**Status:** Complete (2024-12-29)

### Completed Tasks
- ✅ Analyzed current build structure
- ✅ Researched MSBuild SDK patterns
- ✅ Reviewed Microsoft.Build.* SDK examples
- ✅ Mapped import dependencies
- ✅ Identified custom features to preserve
- ✅ Created research documentation
- ✅ Created this roadmap
- ✅ Exhaustively cataloged all properties, items, and targets
- ✅ Documented all conditional logic patterns
- ✅ Analyzed sample project files
- ✅ Mapped complete import chains

### Deliverables
- [sdk-modernization-research.md](./sdk-modernization-research.md)
- [stride-build-properties-inventory.md](./stride-build-properties-inventory.md) ⭐ **NEW**
- This roadmap document

### Key Findings

The inventory reveals **100+ custom MSBuild properties** organized into:
- **Core Framework Properties**: 8 properties defining target frameworks (net10.0, net10.0-windows, net10.0-android, etc.)
- **Platform Properties**: 15 properties for platform detection and configuration (Windows, Linux, macOS, Android, iOS, UWP)
- **Graphics API Properties**: 10 properties for multi-API builds (Direct3D11/12, OpenGL/ES, Vulkan)
- **Assembly Processor Properties**: 15 properties controlling Stride's custom IL processor
- **Build Configuration Properties**: 20+ properties for build control and outputs
- **Package Properties**: 15+ properties for NuGet package generation

**Critical Patterns Identified:**
1. **Two-Tier System**: Stride.Core.* (minimal) vs Stride.* (full engine)
2. **Multi-Targeting**: `StrideRuntime=true` enables automatic platform multi-targeting
3. **Graphics API Inner Builds**: Special handling for building same code with different graphics backends
4. **Solution-Specific Overrides**: Conditional imports based on `$(SolutionName)`
5. **Assembly Processor Integration**: Custom post-compile IL modification step

See [stride-build-properties-inventory.md](./stride-build-properties-inventory.md) for complete details.

## Phase 2: Create Base SDK Structure

**Duration:** 2-3 weeks  
**Goal:** Create minimal working SDK that wraps existing build logic

### Tasks

#### 2.1 Create SDK Project
- [ ] Create `sources/sdk/Stride.Sdk/` directory structure
- [ ] Create `Stride.Sdk.csproj` with PackageType=MSBuildSdk
- [ ] Configure NuGet package metadata (ID, version, description)
- [ ] Set up SDK folder structure (Sdk/, build/, tools/)

#### 2.2 Create Initial Sdk.props
- [ ] Create `Sdk/Sdk.props` as main entry point
- [ ] Import existing Stride.Core.props content
- [ ] Set `UsingStrideSdk` property for detection
- [ ] Configure TargetFramework defaults
- [ ] Set up StridePlatform detection
- [ ] Import Microsoft.NET.Sdk.props at appropriate point

**Key Content:**
```xml
<Project>
  <!-- SDK detection -->
  <PropertyGroup>
    <UsingStrideSdk>true</UsingStrideSdk>
    <StrideSdkVersion>4.3.0</StrideSdkVersion>
  </PropertyGroup>

  <!-- Import supporting props files -->
  <Import Project="Stride.Platforms.props" />
  <Import Project="Stride.Runtime.props" Condition="'$(StrideRuntime)' == 'true'" />
  <Import Project="Stride.Graphics.props" />
  
  <!-- Import base SDK -->
  <Import Project="Sdk.props" Sdk="Microsoft.NET.Sdk" />
</Project>
```

#### 2.3 Create Initial Sdk.targets
- [ ] Create `Sdk/Sdk.targets` as main entry point
- [ ] Import existing Stride.Core.targets content
- [ ] Set up assembly processor targets
- [ ] Configure output path adjustments
- [ ] Import Microsoft.NET.Sdk.targets at appropriate point

**Key Content:**
```xml
<Project>
  <!-- Import base SDK targets -->
  <Import Project="Sdk.targets" Sdk="Microsoft.NET.Sdk" />

  <!-- Import supporting targets files -->
  <Import Project="Stride.AssemblyProcessor.targets" />
  
  <!-- Custom Stride targets -->
  <Target Name="StridePostBuild" AfterTargets="Build">
    <!-- ... -->
  </Target>
</Project>
```

#### 2.4 Create Supporting Files
- [ ] `Sdk/Stride.Platforms.props` - Platform detection logic
- [ ] `Sdk/Stride.Runtime.props` - Multi-targeting logic
- [ ] `Sdk/Stride.Graphics.props` - Graphics API configuration
- [ ] `Sdk/Stride.AssemblyProcessor.targets` - Assembly processing
- [ ] `build/Stride.Sdk.props` - Legacy NuGet wrapper
- [ ] `build/Stride.Sdk.targets` - Legacy NuGet wrapper

#### 2.5 Test Build
- [ ] Build SDK package locally
- [ ] Verify package structure
- [ ] Check Sdk/ folder contents
- [ ] Validate NuGet metadata

### Deliverables
- Working Stride.Sdk NuGet package
- Local NuGet feed for testing
- Basic documentation in SDK README

### Success Criteria
- SDK package builds without errors
- Package has correct structure and metadata
- Can be referenced from a test project

## Phase 3: Pilot Migration

**Duration:** 2-3 weeks  
**Goal:** Test SDK with real project, iterate and fix issues

### Tasks

#### 3.1 Set Up Parallel SDK Support
- [ ] Update global.json with Stride.Sdk reference
- [ ] Set up local NuGet feed in build/packages
- [ ] Create migration helper script (optional)
- [ ] Document migration steps

#### 3.2 Migrate Stride.Core.Mathematics
**Why this project?**
- Small, focused project
- Has StrideRuntime=true flag
- Minimal external dependencies
- Core infrastructure (good test case)

**Steps:**
- [ ] Create backup of original project file
- [ ] Convert to `<Project Sdk="Stride.Sdk">`
- [ ] Remove manual Import statements
- [ ] Remove redundant PropertyGroups
- [ ] Test build locally

#### 3.3 Verify Functionality
- [ ] Build project successfully
- [ ] Run unit tests (if any)
- [ ] Verify assembly processor runs
- [ ] Check output artifacts
- [ ] Test IntelliSense in Visual Studio
- [ ] Test in VS Code with C# extension
- [ ] Test with `dotnet build` CLI

#### 3.4 Iterate and Fix Issues
- [ ] Document any problems encountered
- [ ] Fix SDK issues
- [ ] Update SDK package
- [ ] Re-test until successful

#### 3.5 Migrate Second Project
Choose another project type:
- [ ] Consider Stride.Core (different profile)
- [ ] Or Stride.Assets (different dependencies)
- [ ] Apply same migration process
- [ ] Document differences and issues

### Deliverables
- 1-2 successfully migrated projects
- List of issues encountered and fixed
- Updated SDK package (v0.2.x)
- Migration checklist/guide

### Success Criteria
- Migrated projects build identically to originals
- No loss of functionality
- IntelliSense works
- Unit tests pass

## Phase 4: Cleanup & Optimization

**Duration:** 3-4 weeks  
**Goal:** Consolidate, optimize, and improve the SDK

### Tasks

#### 4.1 Consolidate Property Files
- [ ] Analyze all *.Build.props files
- [ ] Merge common properties into SDK
- [ ] Keep project-specific overrides
- [ ] Remove duplicate property definitions
- [ ] Simplify conditional logic where possible

#### 4.2 Improve Graphics API Handling
- [ ] Streamline StrideGraphicsApi property setup
- [ ] Simplify StrideGraphicsApiDependent builds
- [ ] Optimize output path configuration
- [ ] Better InnerBuild support

#### 4.3 Simplify Platform Detection
- [ ] Refactor StridePlatform logic
- [ ] Consolidate platform-specific defines
- [ ] Improve TargetFramework to Platform mapping
- [ ] Add better validation/error messages

#### 4.4 Optimize TargetFrameworks
- [ ] Review StrideRuntime multi-targeting
- [ ] Simplify TargetFrameworks selection
- [ ] Improve cross-platform build performance
- [ ] Better handling of platform-specific builds

#### 4.5 Add SDK Features
- [ ] Define default ItemGroup includes/excludes
  - `<StrideAsset Include="**/*.sdyaml" />`
  - `<StrideShader Include="**/*.sdsl;**/*.sdfx" />`
- [ ] Consider implicit usings (opt-in)
  - Stride.Core
  - Stride.Core.Mathematics
- [ ] Add SDK-specific MSBuild properties
- [ ] Improve extensibility hooks

#### 4.6 Improve Build Performance
- [ ] Profile build times
- [ ] Optimize import order
- [ ] Cache expensive operations
- [ ] Parallelize where possible

### Deliverables
- Optimized SDK package (v1.0.0-beta)
- Performance comparison report
- Updated documentation
- Migration guide updates

### Success Criteria
- SDK is more maintainable
- Build times are same or better
- Project files are cleaner
- No regressions

## Phase 5: Broader Migration

**Duration:** 4-6 weeks  
**Goal:** Migrate core engine projects

### Tasks

#### 5.1 Migrate Core Projects
Priority order:
- [ ] Stride.Core
- [ ] Stride.Core.Serialization
- [ ] Stride.Core.Assets
- [ ] Stride.Core.Design
- [ ] Stride.Core.MicroThreading
- [ ] Stride.Core.Reflection

#### 5.2 Migrate Engine Projects
- [ ] Stride.Engine
- [ ] Stride.Rendering
- [ ] Stride.Graphics
- [ ] Stride.Physics
- [ ] Stride.Audio
- [ ] Stride.VirtualReality

#### 5.3 Migrate Editor Projects
- [ ] Stride.Assets.Presentation
- [ ] Stride.GameStudio
- [ ] Editor-related projects

#### 5.4 Update Build Scripts
- [ ] Update CI/CD pipelines
- [ ] Update build documentation
- [ ] Update developer setup guides
- [ ] Create troubleshooting guide

#### 5.5 Testing
- [ ] Full engine build
- [ ] Run all unit tests
- [ ] Test sample projects
- [ ] Performance testing
- [ ] Cross-platform validation

### Deliverables
- All core and engine projects migrated
- Updated build scripts
- SDK v1.0.0-rc
- Comprehensive testing report

### Success Criteria
- All projects build successfully
- All tests pass
- No performance regressions
- CI/CD works correctly

## Phase 6: Finalization & Documentation

**Duration:** 2-3 weeks  
**Goal:** Complete migration, stabilize, and document

### Tasks

#### 6.1 Migrate Remaining Projects
- [ ] Sample projects
- [ ] Test projects
- [ ] Tool projects
- [ ] Template projects

#### 6.2 Remove Legacy Code
- [ ] Archive old Stride.props/targets
- [ ] Remove redundant Build.props files
- [ ] Clean up imports in remaining files
- [ ] Update .gitignore if needed

#### 6.3 Documentation
- [ ] **SDK Developer Guide**: How the SDK works internally
- [ ] **Migration Guide**: Step-by-step for any remaining projects
- [ ] **Troubleshooting Guide**: Common issues and solutions
- [ ] **API Reference**: Properties, items, targets exposed by SDK
- [ ] Update contribution guidelines
- [ ] Update build documentation

#### 6.4 Release Preparation
- [ ] Finalize SDK version (1.0.0)
- [ ] Create release notes
- [ ] Prepare blog post/announcement
- [ ] Update main README
- [ ] Tag release in git

#### 6.5 Community Preparation
- [ ] Create migration guide for users
- [ ] Prepare sample projects
- [ ] Update templates
- [ ] Plan support/Q&A

### Deliverables
- Complete migration (all projects)
- Stride.Sdk v1.0.0 (stable)
- Complete documentation suite
- Release announcement

### Success Criteria
- 100% project migration
- Documentation complete
- Ready for public release
- No known critical issues

## Phase 7: Post-Release (Ongoing)

**Duration:** Ongoing  
**Goal:** Maintain, improve, and extend the SDK

### Tasks

#### 7.1 Monitor and Support
- [ ] Track issues on GitHub
- [ ] Respond to community questions
- [ ] Fix bugs as reported
- [ ] Performance monitoring

#### 7.2 Incremental Improvements
- [ ] Based on feedback
- [ ] New features (carefully considered)
- [ ] Performance optimizations
- [ ] Better error messages

#### 7.3 Version Updates
- [ ] Keep aligned with .NET SDK updates
- [ ] Support new target frameworks
- [ ] Deprecate old patterns cleanly
- [ ] Maintain backwards compatibility

### Success Criteria
- Active maintenance
- Community satisfaction
- Stable, reliable SDK
- Clear upgrade path

## Benefits of This Approach

### Immediate Benefits
1. **Tool Compatibility**: Projects work with more tools out-of-the-box
2. **Simplified Projects**: ~50-70% less code per project file
3. **Better IntelliSense**: Improved IDE support
4. **Centralized Logic**: Build logic in one versioned package

### Long-Term Benefits
1. **Easier Maintenance**: Update build logic in one place
2. **Version Control**: SDK can be versioned independently
3. **Better Defaults**: New projects get better starting point
4. **Future-Proof**: Aligned with .NET SDK evolution
5. **Easier Onboarding**: New contributors understand project structure faster

## Risks and Mitigation

### Risk: Breaking Changes
**Mitigation:** 
- Parallel support during transition
- Thorough testing at each phase
- Keep old system working until migration complete
- Easy rollback plan

### Risk: Complex Multi-Targeting
**Mitigation:**
- Preserve existing multi-targeting logic initially
- Incremental optimization
- Extensive testing on all platforms

### Risk: Build Performance
**Mitigation:**
- Profile builds before and after
- Optimize SDK imports
- Benchmark at each phase

### Risk: Learning Curve
**Mitigation:**
- Comprehensive documentation
- Examples and samples
- Community support channels
- Gradual rollout

### Risk: External Dependencies
**Mitigation:**
- Document all dependencies
- Maintain backwards compatibility where possible
- Clear communication about changes

## Success Metrics

### Quantitative
- **Build Performance**: ≤5% slower (ideally faster)
- **Project File Size**: 50-70% reduction
- **Migration Time**: <1 hour per project average
- **Test Pass Rate**: 100% (no regressions)

### Qualitative
- **Developer Satisfaction**: Positive feedback on usability
- **Tool Support**: Works with VS, VS Code, Rider, CLI
- **Maintainability**: Easier to update build logic
- **Documentation**: Comprehensive and clear

## Timeline Summary

| Phase | Duration | Status |
|-------|----------|--------|
| Phase 1: Analysis & Planning | 1 week | ✅ Complete |
| Phase 2: Create Base SDK | 2-3 weeks | Not Started |
| Phase 3: Pilot Migration | 2-3 weeks | Not Started |
| Phase 4: Cleanup & Optimization | 3-4 weeks | Not Started |
| Phase 5: Broader Migration | 4-6 weeks | Not Started |
| Phase 6: Finalization | 2-3 weeks | Not Started |
| Phase 7: Post-Release | Ongoing | Not Started |
| **Total (Phases 1-6)** | **14-20 weeks** | **In Progress** |

**Estimated Completion:** Q2 2025 (conservative estimate)

## Decision Points

Key decisions to make during implementation:

### Decision 1: SDK Name
- **Options:** Stride.Sdk, Stride.Build.Sdk, Microsoft.Stride.Sdk
- **Recommendation:** Stride.Sdk (simple, clear)

### Decision 2: Versioning Strategy
- **Options:** Match engine version, independent versioning
- **Recommendation:** Independent versioning (allows faster iteration)

### Decision 3: Distribution Method
- **Options:** NuGet.org, private feed, both
- **Recommendation:** Both (NuGet.org for releases, private feed for development)

### Decision 4: Breaking Changes Policy
- **Options:** Allow breaking changes, maintain backwards compatibility
- **Recommendation:** Maintain backwards compatibility when reasonable, use version bumps for breaking changes

### Decision 5: Legacy Support
- **Options:** Remove old system immediately, support for N versions
- **Recommendation:** Support both for 1-2 releases, then deprecate old system

## Next Steps

**Immediate (Next Week):**
1. Review and approve this roadmap
2. Create GitHub issue for tracking
3. Set up project board for phases
4. Begin Phase 2 implementation

**Near Term (Next Month):**
1. Complete Phase 2 (Base SDK)
2. Begin Phase 3 (Pilot Migration)
3. Iterate based on findings

**Medium Term (Next Quarter):**
1. Complete Phases 3-4
2. Begin broader migration
3. Document lessons learned

## References

- [SDK Research Document](./sdk-modernization-research.md)
- [Current Stride Build System](../../sources/targets/)
- [.NET SDK Documentation](https://learn.microsoft.com/en-us/dotnet/core/project-sdk/overview)
- [Microsoft.Build.* SDKs](https://github.com/microsoft/MSBuildSdks)

---

**Document Version:** 1.0  
**Last Updated:** December 28, 2025  
**Owner:** Stride Build System Team  
**Status:** Approved / In Planning

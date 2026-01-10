---
name: summarize-session
description: Compact and update SUMMARY.md, keeping only recent session details
---

# Summarize Session Command

Compact SUMMARY.md by summarizing the current session and condensing old information.

## Usage

```
/summarize-session
```

## When to Use

**Trigger:** When context usage reaches ~60% (120k/200k tokens)

This allows room to complete the summary and commit before hitting context limits.

## Compacting Strategy

**CRITICAL:** SUMMARY.md must not grow indefinitely. Each run should:

1. **Latest Session** (Current) - Full detail (new)
2. **Previous Session** - Condensed to 20-30 lines max (keep key achievements/discoveries)
3. **Older Sessions** - Delete entirely

**Goal:** Keep SUMMARY.md under 300 lines total.

## Instructions

Read the existing SUMMARY.md, then REPLACE it with a compacted version containing:

### 1. Header

```markdown
# Session Summary - Stride SDK Migration

**Date:** [Current date]
**Branch:** feature/stride-sdk
**Status:** [Commits ahead/behind origin]

---
```

### 2. Latest Session (Full Detail)

**Section title:** `## Latest Session ([Brief description]) [Status]`

Include full details:
- **Major accomplishments** - What was achieved
- **Commits made** - Hashes and messages
- **Files created/modified** - With line counts
- **Critical discoveries** - Important findings, bugs found, gotchas
- **Verification results** - What was tested and confirmed working
- **Key learnings** - Non-obvious insights for next session

### 3. Previous Session (Condensed)

**Section title:** `## Previous Session - [Brief description]`

Condense the old "Latest Session" to ~20-30 lines:
- 1-2 line summary of what was done
- Key commits (hashes only, no full messages)
- Critical discoveries only (if any)
- Important files created (paths only)

**Example:**
```markdown
## Previous Session - Property Evaluation Analysis

Documented MSBuild SDK evaluation order bug in old build system (sources/targets/Stride.Core.props:58).
Created SDK-PROPERTY-EVALUATION-ANALYSIS.md (400+ lines) and updated documentation.

**Commits:** f0cec9b30, d2427615d
**Key files:** build/docs/SDK-PROPERTY-EVALUATION-ANALYSIS.md, .claude/commands/analyze-csproj-migration.md
```

### 4. Project Status (Current State)

- What's working now
- Current focus area
- Immediate next steps (3-5 items)
- Git status

### 5. Critical Information (Persistent)

**Keep only the most critical information that applies across sessions:**
- Build workflows (NuGet cache clearing)
- Key file locations
- Essential commands
- Property names and their meanings

**Remove:** Session-specific details, old discoveries that are now documented elsewhere.

### 6. Next Steps (Actionable)

List concrete next steps in priority order:
- High priority (next 1-2 sessions)
- Medium priority (3-5 sessions)
- Long-term goals

### 7. Commands for Next Session

Ready-to-run commands:
```bash
# Status check
git status

# Build commands
/build-sdk

# Test commands
dotnet test
```

## Compacting Process

1. **Read existing SUMMARY.md** to understand what was done
2. **Identify sessions:**
   - Current session (being summarized now)
   - Previous session (the old "Latest Session")
   - Older sessions (everything before that)
3. **Write new SUMMARY.md:**
   - Latest Session: Full detail about current work
   - Previous Session: Condense old "Latest Session" to 20-30 lines
   - Delete: All older session details
   - Update: Critical Information and Next Steps sections
4. **Target length:** ~200-300 lines (down from current 595)

## After Creating Summary

1. Verify the compacted summary is complete and under 300 lines
2. Add and commit SUMMARY.md:
   ```bash
   git add SUMMARY.md
   git commit -m "Update session summary with compaction"
   ```
3. Inform the user the summary is ready
4. Suggest they start a new session and begin by reading SUMMARY.md

## Tips for Effective Compaction

**Latest Session (Current work):**
- Full detail - this becomes the reference for next session
- Include WHY decisions were made, not just WHAT
- Document discoveries, bugs, gotchas

**Previous Session (Last session):**
- Reduce to essentials: what was achieved, key commits, critical files
- Remove explanations that are now in permanent documentation
- 20-30 lines maximum

**Older Sessions:**
- Delete entirely - information should be in code comments, commit messages, or permanent docs
- If something is critical across all sessions, move it to "Critical Information"

**Critical Information section:**
- Keep only information needed for ALL future sessions
- Remove session-specific details
- Examples: NuGet cache workflow, MSBuild vs dotnet CLI rules

**Next Steps section:**
- Update based on current progress
- Remove completed items
- Add newly discovered work

## Example Compacted Summary (200-250 lines)

```markdown
# Session Summary - Stride SDK Migration

**Date:** 2026-01-10
**Branch:** feature/stride-sdk
**Status:** 6 commits ahead of origin/feature/stride-sdk

---

## Latest Session (Assembly Processor Implementation) ⏳

### What Was Accomplished

Implemented full Assembly Processor functionality in SDK.

**Commit abc1234: Implement Assembly Processor in Stride.SDK**
- Migrated RunStrideAssemblyProcessor target from old build system
- Added UsingTask declarations and MSBuild logic
- Tested with Stride.Core - serialization now working

**Files Modified:**
- sources/sdk/Stride.Sdk/Sdk/Stride.AssemblyProcessor.targets (stub → full implementation, +180 lines)
- sources/sdk/Stride.Sdk/Stride.Sdk.csproj (added Stride.Core.AssemblyProcessor package reference)

### Verification Results

✅ Stride.Core builds successfully
✅ Assembly processor runs and generates serialization code
✅ Unit tests pass (Stride.Core.Tests)

### Critical Discoveries

**Assembly Processor Dependencies:**
The processor needs `Stride.Core.AssemblyProcessor.exe` from NuGet package, not from local build.
Must reference the package in Stride.Sdk.csproj to deploy with SDK.

### Next Session Focus

Migrate Stride.Core.IO and Stride.Core.Mathematics to SDK format.

---

## Previous Session - SDK Migration Desktop Platforms

Completed Stride.Core SDK migration for desktop platforms. Created Stride.Platform.props/targets,
Stride.AssemblyProcessor.targets (stub), Stride.CodeAnalysis.targets. Multi-targeting verified
working (net10.0, net10.0-windows). Discovered critical NuGet cache clearing requirement.

**Commits:** 63c349108, f0cec9b30
**Key files:** sources/sdk/Stride.Sdk/Sdk/Stride.Platform.{props,targets}

---

## Project Status

**What's Working:**
- ✅ SDK packages build successfully
- ✅ Stride.Core migrated to SDK and builds
- ✅ Multi-targeting (desktop platforms)
- ✅ Assembly processor functional

**Current Focus:**
Migrating additional core projects to SDK format

**Immediate Next Steps:**
1. Migrate Stride.Core.IO to SDK
2. Migrate Stride.Core.Mathematics to SDK
3. Test full solution build with migrated projects
4. Add mobile platform support (Phase 2)

**Git Status:** Clean (all changes committed)

---

## Critical Information

### NuGet Cache Management

**CRITICAL:** After modifying SDK source, MUST clear NuGet cache:

```bash
rmdir /s /q "C:\Users\musse\.nuget\packages\stride.sdk" 2>nul
rmdir /s /q "C:\Users\musse\.nuget\packages\stride.sdk.runtime" 2>nul
```

Then rebuild SDK and restore consuming projects.

### Build Tools

- **MSBuild:** Use for full solution builds (C++/CLI projects)
  - Path: `C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe`
- **dotnet CLI:** Use for SDK building, individual C# projects, tests

### Key Build Properties

- `StridePlatform` - Current platform (Windows, Linux, etc.)
- `StridePlatforms` - List of target platforms
- `StrideRuntime=true` - Auto-generates TargetFrameworks for multi-platform
- `StrideGraphicsApi` - Current graphics API
- `StrideGraphicsApiDependent=true` - Enables graphics API multi-targeting
- `StrideAssemblyProcessor` - Enable assembly processing

### MSBuild SDK Evaluation Order

```
Sdk.props (before .csproj) → .csproj (user properties) → Sdk.targets (after .csproj)
```

**Rule:** Defaults in props, conditional logic in targets.

### File Locations

**SDK Source:**
- sources/sdk/Stride.Sdk/ (SDK package)
- sources/sdk/Stride.Sdk/Sdk/{Sdk.props, Sdk.targets}
- sources/sdk/Stride.Sdk/Sdk/Stride.Platform.{props,targets}
- sources/sdk/Stride.Sdk/Sdk/Stride.AssemblyProcessor.targets
- sources/sdk/Stride.Sdk/Sdk/Stride.Frameworks.targets

**Documentation:**
- CLAUDE.md - Project guidance
- build/docs/SDK-WORK-GUIDE.md - SDK development workflow
- build/docs/SDK-PROPERTY-EVALUATION-ANALYSIS.md - Property evaluation analysis

**Migrated Projects:**
- sources/core/Stride.Core/Stride.Core.csproj (SDK-style)

**Old Build System (being replaced):**
- sources/targets/*.props, *.targets (17 files)

---

## Next Steps

### High Priority (1-2 Sessions)

1. **Migrate Stride.Core.IO**
   - Use `/analyze-csproj-migration` first
   - Follow Stride.Core migration pattern
   - Verify tests pass

2. **Migrate Stride.Core.Mathematics**
   - Similar structure to Stride.Core
   - Should be straightforward

3. **Test Full Solution Build**
   - Verify SDK changes don't break other projects
   - Run full test suite

### Medium Priority (3-5 Sessions)

1. **Add Mobile/UWP Platform Support (Phase 2)**
   - Uncomment Phase 2 sections in Platform.props/targets
   - Test on Android/iOS builds

2. **Remove Unused Properties**
   - StrideBuildTags, RestorePackages identified as unused
   - Clean up during migration

### Long-Term

1. Complete SDK migration for all projects
2. Remove old build system (sources/targets/)
3. Update project templates
4. Advanced features (native libs, auto-pack, localization)

---

## Commands for Next Session

```bash
# Check status
git status
git log --oneline -5

# Build SDK
/build-sdk
# OR manually:
dotnet build sources\sdk\Stride.Sdk.slnx
rmdir /s /q "C:\Users\musse\.nuget\packages\stride.sdk" 2>nul

# Build Stride.Core
dotnet restore sources\core\Stride.Core\Stride.Core.csproj
dotnet build sources\core\Stride.Core\Stride.Core.csproj

# Run tests
dotnet test sources\core\Stride.Core.Tests\Stride.Core.Tests.csproj

# Analyze project for migration
/analyze-csproj-migration sources/core/Stride.Core.IO/Stride.Core.IO.csproj
```

---

**For resuming work:** Read "Latest Session" first for current context. SDK migration is progressing well - Stride.Core complete with Assembly Processor working. Next focus is migrating additional core projects.
```

---

**Remember:**
- Keep SUMMARY.md under 300 lines
- Latest session = full detail
- Previous session = 20-30 line summary
- Delete older sessions
- Update Critical Information and Next Steps based on progress

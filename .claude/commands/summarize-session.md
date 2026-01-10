---
name: summarize-session
description: Create SUMMARY.md for session handoff when context reaches 60%+
---

# Summarize Session Command

Create a comprehensive SUMMARY.md file to hand off context to a new Claude session.

## Usage

```
/summarize-session
```

## When to Use

**Trigger:** When context usage reaches ~60% (120k/200k tokens)

This allows room to complete the summary and commit before hitting context limits.

## Instructions

Create or update `SUMMARY.md` in the repository root with the following sections:

### 1. Header

```markdown
# Session Summary - [Project/Feature Name]

**Date:** [Current date]
**Branch:** [Current git branch]
**Status:** [Commits ahead/behind origin]
```

### 2. What Was Accomplished

List all work completed in this session:
- Files created/modified
- Features implemented
- Documentation added
- Issues resolved
- Commits made (with commit hashes and messages)

### 3. Critical Information

Document any critical workflows, gotchas, or non-obvious patterns discovered:
- Build workflows
- Testing procedures
- Known issues and workarounds
- Important file locations
- Key commands

### 4. Current State

Describe the current state:
- What's working
- What's in progress
- What's broken or blocked
- Git status (staged/unstaged changes)

### 5. Unfinished Items / Next Steps

List concrete next steps:
- TODOs from code comments
- Planned work items
- Open questions
- Dependencies to resolve

### 6. File Locations Reference

Map of important files and their purposes:
- Configuration files
- Key source files
- Documentation
- Build artifacts

### 7. Commands for Next Session

Provide ready-to-run commands for common tasks:
```bash
# Check status
git status
git log --oneline -5

# Build commands
[specific build commands]

# Test commands
[specific test commands]
```

### 8. Context Notes

- Current context token usage
- Any context-heavy operations performed
- Recommendations for next session

## After Creating Summary

1. Review the summary for completeness
2. Add and commit SUMMARY.md:
   ```bash
   git add SUMMARY.md
   git commit -m "Add session summary for context handoff"
   ```
3. Inform the user the summary is ready
4. Suggest they start a new session and begin by reading SUMMARY.md

## Tips for Effective Summaries

**Be specific:**
- Include actual file paths, not just "some config file"
- Include exact commands, not just "run the build"
- Reference commit hashes when discussing changes

**Be comprehensive:**
- Assume the next session knows nothing about this session
- Explain WHY decisions were made, not just WHAT was done
- Document any non-obvious patterns or conventions

**Be actionable:**
- Provide clear next steps
- Include commands ready to copy-paste
- Link to relevant documentation

**Be honest:**
- Note any hacks or temporary solutions
- Document known issues
- Mention anything that seems wrong but wasn't investigated

## Example Summary Structure

```markdown
# Session Summary - Authentication Feature

**Date:** 2026-01-10
**Branch:** feature/auth
**Status:** 5 commits ahead of main

## What Was Accomplished

1. Implemented JWT authentication (commits abc123, def456)
   - Added JwtService in src/auth/JwtService.cs
   - Updated UserController with [Authorize] attributes

2. Created authentication tests
   - src/tests/AuthTests.cs (12 tests, all passing)

3. Updated documentation
   - docs/API.md - Added authentication section
   - README.md - Added setup instructions

## Critical Information

**JWT Secret Storage:**
IMPORTANT: JWT secret is read from appsettings.json -> "Jwt:Secret"
Must be at least 32 characters for HS256.

**Development secret:** Located in appsettings.Development.json
**Production secret:** Set via environment variable JWT_SECRET

## Current State

- ✅ JWT generation and validation working
- ✅ All auth tests passing
- ⏳ Refresh token implementation in progress
- ❌ Password reset email not yet implemented

Git status: Clean working directory, all changes committed

## Unfinished Items / Next Steps

1. Implement refresh token rotation
   - Add RefreshToken table to database
   - Update JwtService with refresh logic
   - See TODO in src/auth/JwtService.cs:156

2. Add password reset flow
   - Email service integration (SendGrid?)
   - Reset token generation
   - UI for reset page

## File Locations Reference

**Core Auth:**
- src/auth/JwtService.cs - JWT generation/validation
- src/auth/AuthController.cs - Login/register endpoints
- src/middleware/JwtMiddleware.cs - Request authentication

**Configuration:**
- appsettings.json - JWT settings (secret, expiry)
- src/Program.cs:45 - Auth middleware registration

**Tests:**
- src/tests/AuthTests.cs - Unit tests
- src/tests/integration/AuthFlowTests.cs - Integration tests

## Commands for Next Session

```bash
# Build and run
dotnet build
dotnet run

# Run auth tests
dotnet test --filter "Category=Auth"

# Check JWT config
cat appsettings.Development.json | grep -A5 "Jwt"
```

## Context Notes

- Session ended at 125k tokens (62% usage)
- Heavy file reading for understanding existing auth patterns
- Next session: Focus on refresh token implementation
```

---

**Remember:** This summary is a gift to your future self (or another Claude instance). Make it detailed and actionable!

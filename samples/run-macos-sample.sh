#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
STRIDE_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
COMMAND="${1:-}"

usage() {
  cat >&2 <<EOF
Usage:
  $0 list
  $0 <sample-name|path/to/Sample.macOS.csproj> [app arguments...]

Environment:
  CONFIGURATION=Debug|Release   Build configuration. Default: Debug
  BUILD_ONLY=true               Build but do not launch
  SKIP_BUILD=true               Launch existing output without building
  BUILD_VERBOSITY=quiet         dotnet build verbosity. Default: quiet
  SHOW_BUILD_WARNINGS=true      Show build warnings. Default: false
  NUGET_PACKAGES=/path          Package cache. Default: .nuget-samples
  SDL_VULKAN_LIBRARY=/path      Vulkan loader dylib override
  VK_DRIVER_FILES=/path         Vulkan ICD JSON override
EOF
}

sample_projects() {
  find "$SCRIPT_DIR" -name '*.macOS.csproj' -type f \
    ! -path "$SCRIPT_DIR/NewGame/*" \
    ! -path "$SCRIPT_DIR/.generated-macos-launchers/*" \
    | sort
}

windows_projects() {
  find "$SCRIPT_DIR" -name '*.Windows.csproj' -type f \
    ! -path "$SCRIPT_DIR/NewGame/*" \
    ! -path "$SCRIPT_DIR/.generated-macos-launchers/*" \
    | sort
}

sample_name() {
  basename "$1" .macOS.csproj
}

windows_sample_name() {
  basename "$1" .Windows.csproj
}

list_samples() {
  sample_projects | while IFS= read -r project; do
    name="$(sample_name "$project")"
    rel="${project#$STRIDE_ROOT/}"
    printf '%-24s %s\n' "$name" "$rel"
  done

  windows_projects | while IFS= read -r project; do
    name="$(windows_sample_name "$project")"
    if sample_projects | grep -q "/$name.macOS/$name.macOS.csproj$"; then
      continue
    fi
    rel="${project#$STRIDE_ROOT/}"
    printf '%-24s %s %s\n' "$name" "(generated)" "$rel"
  done
}

resolve_path() {
  local base_dir="$1"
  local path="$2"

  path="${path//\\//}"
  if [[ "$path" == /* ]]; then
    printf '%s\n' "$path"
  else
    printf '%s/%s\n' "$base_dir" "$path"
  fi
}

relative_path() {
  local from_dir="$1"
  local target="$2"

  python3 - "$from_dir" "$target" <<'PY'
import os
import sys
print(os.path.relpath(os.path.abspath(sys.argv[2]), os.path.abspath(sys.argv[1])).replace(os.sep, "/"))
PY
}

generated_launcher_for_windows_project() {
  local windows_project="$1"
  local name win_dir project_ref game_project game_dir sdpkg app_source generated_dir generated_project

  name="$(windows_sample_name "$windows_project")"
  win_dir="$(cd "$(dirname "$windows_project")" && pwd)"
  project_ref="$(sed -n 's/.*<ProjectReference Include="\([^"]*\)".*/\1/p' "$windows_project" | head -n 1)"
  if [[ -z "$project_ref" ]]; then
    echo "Cannot generate macOS launcher; no ProjectReference found in $windows_project" >&2
    return 1
  fi

  game_project="$(resolve_path "$win_dir" "$project_ref")"
  if [[ ! -f "$game_project" ]]; then
    echo "Cannot generate macOS launcher; game project not found: $game_project" >&2
    return 1
  fi

  game_dir="$(cd "$(dirname "$game_project")" && pwd)"
  sdpkg="$(find "$game_dir" -maxdepth 1 -name '*.sdpkg' -type f | sort | head -n 1)"
  if [[ -z "$sdpkg" ]]; then
    echo "Cannot generate macOS launcher; no .sdpkg found in $game_dir" >&2
    return 1
  fi

  app_source="$(find "$win_dir" -maxdepth 1 -name '*App.cs' ! -name '*.DemoApp.cs' -type f | sort | head -n 1)"
  if [[ -z "$app_source" ]]; then
    echo "Cannot generate macOS launcher; no app entry point found in $win_dir" >&2
    return 1
  fi

  generated_dir="$SCRIPT_DIR/.generated-macos-launchers/${windows_project#$SCRIPT_DIR/}"
  generated_dir="${generated_dir%.Windows.csproj}.macOS"
  generated_project="$generated_dir/$name.macOS.csproj"
  mkdir -p "$generated_dir"

  local output_path project_ref_rel package_rel app_rel
  output_path="$(relative_path "$generated_dir" "$(cd "$win_dir/.." && pwd)/Bin/macOS/\$(Configuration)")"
  project_ref_rel="$(relative_path "$generated_dir" "$game_project")"
  package_rel="$(relative_path "$generated_dir" "$sdpkg")"
  app_rel="$generated_dir/$(basename "$app_source")"

  cat > "$generated_project" <<EOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Exe</OutputType>
    <RootNamespace>$name</RootNamespace>
    <OutputPath>$output_path/</OutputPath>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
    <DefineConstants>STRIDE_PLATFORM_DESKTOP</DefineConstants>
  </PropertyGroup>

  <PropertyGroup>
    <StrideCurrentPackagePath>\$(MSBuildThisFileDirectory)$package_rel</StrideCurrentPackagePath>
    <StridePlatform>macOS</StridePlatform>
    <StrideGraphicsApi>Vulkan</StrideGraphicsApi>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="\$(MSBuildThisFileDirectory)$project_ref_rel" />
  </ItemGroup>
</Project>
EOF

  cp "$app_source" "$app_rel"
  printf '%s\n' "$generated_project"
}

resolve_project() {
  local query="$1"
  local candidate

  if [[ "$query" == /* && -f "$query" ]]; then
    printf '%s\n' "$query"
    return 0
  fi

  if [[ -f "$query" ]]; then
    printf '%s/%s\n' "$(cd "$(dirname "$query")" && pwd)" "$(basename "$query")"
    return 0
  fi

  candidate="$STRIDE_ROOT/$query"
  if [[ -f "$candidate" ]]; then
    printf '%s\n' "$candidate"
    return 0
  fi

  local matches=()
  local lower_query
  lower_query="$(printf '%s' "$query" | tr '[:upper:]' '[:lower:]')"
  while IFS= read -r project; do
    local name lower_name
    name="$(sample_name "$project")"
    lower_name="$(printf '%s' "$name" | tr '[:upper:]' '[:lower:]')"
    if [[ "$lower_name" == "$lower_query" || "$lower_name" == *"$lower_query"* ]]; then
      matches+=("$project")
    fi
  done < <(sample_projects)

  if [[ ${#matches[@]} -eq 0 ]]; then
    while IFS= read -r project; do
      local name lower_name
      name="$(windows_sample_name "$project")"
      lower_name="$(printf '%s' "$name" | tr '[:upper:]' '[:lower:]')"
      if [[ "$lower_name" == "$lower_query" || "$lower_name" == *"$lower_query"* ]]; then
        matches+=("$project")
      fi
    done < <(windows_projects)
  fi

  if [[ ${#matches[@]} -eq 1 ]]; then
    if [[ "${matches[0]}" == *.Windows.csproj ]]; then
      generated_launcher_for_windows_project "${matches[0]}"
    else
      printf '%s\n' "${matches[0]}"
    fi
    return 0
  fi

  if [[ ${#matches[@]} -gt 1 ]]; then
    echo "Ambiguous sample '$query'. Matches:" >&2
    for project in "${matches[@]}"; do
      echo "  $(sample_name "$project")" >&2
    done
    return 2
  fi

  echo "Sample not found: $query" >&2
  echo >&2
  echo "Available samples:" >&2
  list_samples >&2
  return 2
}

if [[ -z "$COMMAND" || "$COMMAND" == "-h" || "$COMMAND" == "--help" ]]; then
  usage
  exit 2
fi

if [[ "$COMMAND" == "list" ]]; then
  list_samples
  exit 0
fi

PROJECT="$(resolve_project "$COMMAND")"
shift

CONFIGURATION="${CONFIGURATION:-Debug}"
PACKAGE_CACHE="${NUGET_PACKAGES:-$STRIDE_ROOT/.nuget-samples}"
BUILD_VERBOSITY="${BUILD_VERBOSITY:-quiet}"

if [[ "${SKIP_BUILD:-false}" != "true" ]]; then
  build_logger_args=()
  if [[ "${SHOW_BUILD_WARNINGS:-false}" != "true" ]]; then
    build_logger_args=(-clp:ErrorsOnly\;NoSummary)
  fi

  build_cmd=(dotnet build "$PROJECT" \
    --packages "$PACKAGE_CACHE" \
    -p:Configuration="$CONFIGURATION" \
    -p:NuGetAudit=false \
    -p:GeneratePackageOnBuild=false \
    -p:NoWarn=0162 \
    -v:"$BUILD_VERBOSITY")

  if [[ ${#build_logger_args[@]} -gt 0 ]]; then
    build_cmd+=("${build_logger_args[@]}")
  fi

  if [[ "${SHOW_BUILD_WARNINGS:-false}" == "true" ]]; then
    "${build_cmd[@]}"
  else
    build_log="$(mktemp)"
    if ! "${build_cmd[@]}" >"$build_log" 2>&1; then
      cat "$build_log" >&2
      rm -f "$build_log"
      exit 1
    fi
    rm -f "$build_log"
  fi
fi

TARGET_DIR="$(dotnet msbuild "$PROJECT" -nologo -getProperty:TargetDir -p:Configuration="$CONFIGURATION")"
ASSEMBLY_NAME="$(dotnet msbuild "$PROJECT" -nologo -getProperty:AssemblyName -p:Configuration="$CONFIGURATION")"
EXE="$TARGET_DIR/$ASSEMBLY_NAME"

if [[ ! -x "$EXE" ]]; then
  echo "Executable not found: $EXE" >&2
  exit 1
fi

if [[ "${BUILD_ONLY:-false}" == "true" ]]; then
  echo "$EXE"
  exit 0
fi

if [[ -z "${SDL_VULKAN_LIBRARY:-}" && -f /opt/homebrew/lib/libvulkan.1.dylib ]]; then
  export SDL_VULKAN_LIBRARY=/opt/homebrew/lib/libvulkan.1.dylib
fi

if [[ -z "${VK_DRIVER_FILES:-}" && -f /opt/homebrew/etc/vulkan/icd.d/MoltenVK_icd.json ]]; then
  export VK_DRIVER_FILES=/opt/homebrew/etc/vulkan/icd.d/MoltenVK_icd.json
fi

if [[ -z "${VK_ICD_FILENAMES:-}" && -n "${VK_DRIVER_FILES:-}" ]]; then
  export VK_ICD_FILENAMES="$VK_DRIVER_FILES"
fi

cd "$TARGET_DIR"
exec "$EXE" "$@"

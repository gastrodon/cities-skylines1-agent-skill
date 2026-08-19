#!/usr/bin/env bash
# Linux build for SkylinesAgentBridge.dll using Mono's `mcs` compiler against the
# native Linux Cities: Skylines managed assemblies. Mirrors scripts/build.ps1,
# which targets Windows csc.exe instead. Run inside `nix develop` (see flake.nix)
# or anywhere `mcs` is on PATH.
set -euo pipefail

repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
src="$repo/src"
out="$repo/bin"

game="${CS1_GAME_DIR:-$HOME/.local/share/Steam/steamapps/common/Cities_Skylines}"
managed="$game/Cities_Data/Managed"

if [ ! -f "$managed/ICities.dll" ]; then
  echo "Cities: Skylines managed DLLs were not found at $managed" >&2
  echo "Set CS1_GAME_DIR to override the install path." >&2
  exit 1
fi

if ! command -v mcs >/dev/null 2>&1; then
  echo "mcs (Mono C# compiler) was not found on PATH. Run inside 'nix develop'." >&2
  exit 1
fi

mkdir -p "$out"
target="$out/SkylinesAgentBridge.dll"

sources=("$src"/*.cs)

mcs \
  -target:library \
  -out:"$target" \
  -debug:portable \
  -optimize+ \
  -define:TRACE \
  -reference:"$managed/ICities.dll" \
  -reference:"$managed/Assembly-CSharp.dll" \
  -reference:"$managed/Assembly-CSharp-firstpass.dll" \
  -reference:"$managed/ColossalManaged.dll" \
  -reference:"$managed/UnityEngine.dll" \
  "${sources[@]}"

echo "Built $target"

mod_dir="${CS1_MOD_DIR:-$HOME/.local/share/Colossal Order/Cities_Skylines/Addons/Mods/SkylinesAgentBridge}"
mkdir -p "$mod_dir"
cp -f "$target" "$mod_dir/SkylinesAgentBridge.dll"
echo "Copied to $mod_dir"

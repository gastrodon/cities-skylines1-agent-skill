{
  description = "Build + dev shell for the Cities: Skylines 1 Agent Bridge mod.";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
    flake-utils.url = "github:numtide/flake-utils";
  };

  outputs = { self, nixpkgs, flake-utils }:
    flake-utils.lib.eachDefaultSystem (system:
      let
        pkgs = import nixpkgs { inherit system; };

        # The mod links against Cities: Skylines' own managed assemblies, which are
        # proprietary game files that live outside the Nix store (in the local Steam
        # install) and can't be fetched/pinned like a normal build input. Their
        # location is read from $CS1_GAME_DIR (falling back to the default Steam
        # path), which means evaluating this flake's `packages.default` requires
        # `--impure` (see the `build` app below for the common case). Once the
        # relevant DLLs are picked up, the actual compile happens in the normal
        # sandbox: `builtins.path` copies just those five files into the store as a
        # fixed input, so the derivation itself is a plain, reproducible build.
        gameDirEnv = builtins.getEnv "CS1_GAME_DIR";
        gameDir =
          if gameDirEnv != "" then gameDirEnv
          else "${builtins.getEnv "HOME"}/.local/share/Steam/steamapps/common/Cities_Skylines";
        managedDir = "${gameDir}/Cities_Data/Managed";

        managedAssemblies = [
          "ICities.dll"
          "Assembly-CSharp.dll"
          "Assembly-CSharp-firstpass.dll"
          "ColossalManaged.dll"
          "UnityEngine.dll"
        ];

        managedSrc = builtins.path {
          path = managedDir;
          name = "cs1-managed-dlls";
          filter = path: type:
            type == "regular" && builtins.elem (builtins.baseNameOf path) managedAssemblies;
        };

        skylinesAgentBridge = pkgs.stdenv.mkDerivation {
          pname = "SkylinesAgentBridge";
          version = "0.0.0";
          src = ./src;

          nativeBuildInputs = [ pkgs.mono ];
          dontConfigure = true;

          buildPhase = ''
            runHook preBuild
            # -sdk:2 targets the .NET 2.0/3.5 profile (ImageRuntimeVersion v2.0.50727).
            # CS1 embeds a Mono runtime of that vintage; plain mcs defaults to stamping
            # v4.0.30319, which the game's loader rejects with a TypeLoadException on
            # any type that touches enough of the BCL (see git log for the postmortem,
            # and upstream issue #8 for the equivalent Windows csc.exe failure mode).
            mcs \
              -sdk:2 \
              -target:library \
              -out:SkylinesAgentBridge.dll \
              -optimize+ \
              -define:TRACE \
              -reference:${managedSrc}/ICities.dll \
              -reference:${managedSrc}/Assembly-CSharp.dll \
              -reference:${managedSrc}/Assembly-CSharp-firstpass.dll \
              -reference:${managedSrc}/ColossalManaged.dll \
              -reference:${managedSrc}/UnityEngine.dll \
              *.cs
            runHook postBuild
          '';

          installPhase = ''
            runHook preInstall
            mkdir -p "$out"
            cp SkylinesAgentBridge.dll "$out/"
            runHook postInstall
          '';
        };

        modDirEnv = builtins.getEnv "CS1_MOD_DIR";
        modDir =
          if modDirEnv != "" then modDirEnv
          else "${builtins.getEnv "HOME"}/.local/share/Colossal Order/Cities_Skylines/Addons/Mods/SkylinesAgentBridge";

        deployScript = pkgs.writeShellApplication {
          name = "skylines-agent-bridge-deploy";
          runtimeInputs = [ pkgs.nix ];
          text = ''
            set -euo pipefail
            echo "Building SkylinesAgentBridge.dll (nix build, --impure for \$CS1_GAME_DIR)..."
            nix build --impure "${toString ./.}#default" -o /tmp/skylines-agent-bridge-result
            mkdir -p "${modDir}"
            cp -f /tmp/skylines-agent-bridge-result/SkylinesAgentBridge.dll "${modDir}/SkylinesAgentBridge.dll"
            echo "Deployed to ${modDir}/SkylinesAgentBridge.dll"
          '';
        };
      in
      {
        packages.default = skylinesAgentBridge;

        apps.default = {
          type = "app";
          program = "${deployScript}/bin/skylines-agent-bridge-deploy";
        };

        devShells.default = pkgs.mkShell {
          name = "skylines-agent-bridge";

          packages = with pkgs; [
            mono          # provides `mcs`, the C# compiler used to build SkylinesAgentBridge.dll
                          # against the game's Mono-compatible managed assemblies.
            ilspycmd      # .NET IL decompiler, used to read Assembly-CSharp.dll / ColossalManaged.dll
                          # to find fields and methods not documented anywhere (e.g. VehicleManager
                          # traffic flow, Starter.cs command line options).
            jq
            curl
          ];

          shellHook = ''
            echo "Skylines Agent Bridge dev shell."
            echo "  Build (script):     ./scripts/build.sh"
            echo "  Build (flake):      nix build --impure   # -> result/SkylinesAgentBridge.dll"
            echo "  Build + deploy:     nix run --impure .   # -> copies into Addons/Mods/..."
            echo "  Decompile:          ilspycmd -t <TypeName> -o /tmp/decompiled <dll>"
            echo "  Game managed dlls: \$CS1_MANAGED (see scripts/build.sh for default path)"
          '';
        };
      });
}

{
  description = "Dev shell for the Cities: Skylines 1 Agent Bridge mod: build (mono mcs) + reverse engineering (ilspycmd) tools.";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
    flake-utils.url = "github:numtide/flake-utils";
  };

  outputs = { self, nixpkgs, flake-utils }:
    flake-utils.lib.eachDefaultSystem (system:
      let
        pkgs = import nixpkgs { inherit system; };
      in
      {
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
            echo "  Build:      ./scripts/build.sh"
            echo "  Decompile:  ilspycmd -t <TypeName> -o /tmp/decompiled <dll>"
            echo "  Game managed dlls: \$CS1_MANAGED (see scripts/build.sh for default path)"
          '';
        };
      });
}

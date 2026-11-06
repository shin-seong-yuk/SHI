#!/usr/bin/env bash
# Compiles SHI.Core with Unity's bundled Roslyn and runs the auto-player. Works while the editor is open.
set -u
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
D="/c/Program Files/Unity/Hub/Editor/6000.3.19f1/Editor/Data"
V="$(ls "$D/NetCoreRuntime/shared/Microsoft.NETCore.App/" | head -1)"
FW="$D/NetCoreRuntime/shared/Microsoft.NETCore.App/$V"
OUT="${TEMP:-/tmp}/shicheck"
mkdir -p "$OUT"
RSP="$OUT/check.rsp"
{
  echo "-nologo -nostdlib -langversion:9 -target:exe"
  echo "-out:\"$(cygpath -m "$OUT/check.dll")\""
  for f in "$FW"/*.dll; do
    case "$(basename "$f")" in
      *.Native.dll) ;;
      System.*|netstandard.dll|mscorlib.dll|Microsoft.CSharp.dll) echo "-r:\"$(cygpath -m "$f")\"" ;;
    esac
  done
  echo "\"$(cygpath -m "$ROOT/Tools/CoreCheck/Program.cs")\""
  for f in "$ROOT"/Assets/SHI/Core/*.cs; do echo "\"$(cygpath -m "$f")\""; done
} > "$RSP"
"$D/NetCoreRuntime/dotnet.exe" "$(cygpath -m "$D/DotNetSdkRoslyn/csc.dll")" "@$(cygpath -m "$RSP")" 2>&1 | grep -v "warning CS1701\|native" | cut -c1-220 | head -30
printf '{"runtimeOptions":{"tfm":"net6.0","framework":{"name":"Microsoft.NETCore.App","version":"%s"}}}' "$V" > "$OUT/check.runtimeconfig.json"
"$D/NetCoreRuntime/dotnet.exe" "$(cygpath -m "$OUT/check.dll")"

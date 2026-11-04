#!/usr/bin/env bash
# Type-check harness runner. Compiles the game scripts against Unity API stubs
# and prints the error count per configuration. This is NOT a Unity build.
# Usage: tools/verify/run.sh [original|runtime|inputsystem|editor|all]
set -u
cd "$(dirname "$0")"
which dotnet >/dev/null 2>&1 || export PATH="$HOME/.dotnet:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
target="${1:-all}"
status=0
run() {
  local name="$1" proj="$2"
  local log="../../.verify-build/$name.log"
  mkdir -p ../../.verify-build
  dotnet build "$proj" -nologo --no-incremental -clp:NoSummary -v:n > "$log" 2>&1
  local errors warnings
  errors=$(grep -E ': error ' "$log" | sort -u | wc -l)
  warnings=$(grep -E ': warning CS' "$log" | grep -v '/UnityStubs/' | sort -u | wc -l)
  echo "$name: $errors error(s), $warnings warning(s) in game code  (log: .verify-build/$name.log)"
  [ "$errors" -eq 0 ] || status=1
}
case "$target" in
  original) run original Harness.Original.csproj ;;
  runtime) run runtime Harness.Runtime.csproj ;;
  inputsystem) run inputsystem Harness.InputSystem.csproj ;;
  editor) run editor Harness.Editor.csproj ;;
  all)
    run runtime Harness.Runtime.csproj
    run inputsystem Harness.InputSystem.csproj
    run editor Harness.Editor.csproj ;;
esac
exit $status

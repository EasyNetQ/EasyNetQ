#!/usr/bin/env bash
# Build the fixture like a consuming app's CI does and fail when an EasyNetQ project is built more than once or as
# Debug (see README.md): once as a solution, once as the standalone App.Smoke (reaches EasyNetQ only through App,
# without importing the props, like a test project built with dotnet test --project). Run from anywhere.
set -euo pipefail
cd "$(dirname "$0")"
source=../../Source
status=0

clean() {
  rm -rf App/bin App/obj App.Tests/bin App.Tests/obj App.Smoke/bin App.Smoke/obj
  for p in "$source"/EasyNetQ*/; do rm -rf "$p/bin" "$p/obj"; done
}

check() {
  local label=$1 log=$2
  for project in EasyNetQ EasyNetQ.Core EasyNetQ.RabbitMQ EasyNetQ.Transport.InMemory EasyNetQ.Generators; do
    builds=$(grep -cE "^\s+$project -> " "$log" || true)
    echo "$label: $project built $builds time(s)"
    if [ "$builds" -gt 1 ]; then status=1; fi
  done
  if ls -d "$source"/EasyNetQ*/bin/Debug >/dev/null 2>&1; then
    echo "$label: an EasyNetQ project was built as Debug in a Release build:"; ls -d "$source"/EasyNetQ*/bin/Debug
    status=1
  fi
}

clean
dotnet build consumer.slnx -c Release -v n -nologo | tee build.log
check solution build.log

clean
dotnet build App.Smoke/App.Smoke.csproj -c Release -v n -nologo | tee build-smoke.log
check standalone build-smoke.log

exit $status

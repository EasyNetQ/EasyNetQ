#!/usr/bin/env bash
# Build the fixture like a consuming app's CI does and fail when an EasyNetQ project is built more than once or as
# Debug (see README.md). Run from anywhere.
set -euo pipefail
cd "$(dirname "$0")"
source=../../Source
rm -rf App/bin App/obj App.Tests/bin App.Tests/obj
for p in "$source"/EasyNetQ*/; do rm -rf "$p/bin" "$p/obj"; done
dotnet build consumer.slnx -c Release -v n -nologo | tee build.log
status=0
for project in EasyNetQ EasyNetQ.Core EasyNetQ.RabbitMQ EasyNetQ.Transport.InMemory EasyNetQ.Generators; do
  builds=$(grep -cE "^\s+$project -> " build.log || true)
  echo "$project: built $builds time(s)"
  if [ "$builds" -ne 1 ]; then status=1; fi
done
if ls -d "$source"/EasyNetQ*/bin/Debug >/dev/null 2>&1; then
  echo "an EasyNetQ project was built as Debug in a Release build:"; ls -d "$source"/EasyNetQ*/bin/Debug
  status=1
fi
exit $status

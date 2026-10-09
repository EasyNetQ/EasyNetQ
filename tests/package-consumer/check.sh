#!/usr/bin/env bash
# Pack EasyNetQ, then Native AOT publish the AOT smoke sample against the packed packages (not project references)
# and run it against a broker. Fails on any trim/AOT warning or when the sample's checks fail; a missing source
# generator shows up as a runtime failure, since reflection fallbacks are off under AOT.
# Usage: check.sh [connectionString]   (default: $EASYNETQ_CONNECTION or host=localhost)
set -euo pipefail
cd "$(dirname "$0")"
version=9.0.0-local.$(date +%s)
rid=$(dotnet --info | sed -n 's/^ *RID: *//p' | head -1)

rm -rf packages bin obj out
# build, then pack --no-build, like publish-to-nuget: a cold `dotnet pack` races GeneratePackageOnBuild across TFMs
dotnet build ../../Source/EasyNetQ.slnx -c Release -p:MinVerVersionOverride="$version" -nologo -v q
dotnet pack ../../Source/EasyNetQ.slnx -c Release --no-build -p:MinVerVersionOverride="$version" -p:PackageOutputPath="$PWD/packages" -nologo -v q
for package in EasyNetQ EasyNetQ.Core EasyNetQ.RabbitMQ EasyNetQ.Transport.InMemory EasyNetQ.AspNetCore.SignalR; do
  test -f "packages/$package.$version.nupkg" || { echo "missing $package package"; exit 1; }
done
# captured first: grep -q would close the pipe early and fail unzip under pipefail
listing=$(unzip -l "packages/EasyNetQ.Core.$version.nupkg")
grep -q "analyzers/dotnet/cs/EasyNetQ.Generators.dll" <<<"$listing" \
  || { echo "EasyNetQ.Core does not ship the source generator"; exit 1; }
readme=$(unzip -p "packages/EasyNetQ.AspNetCore.SignalR.$version.nupkg" README.md | head -1)
grep -qi "signalr" <<<"$readme" || { echo "EasyNetQ.AspNetCore.SignalR ships the repository README, not its own"; exit 1; }

dotnet publish PackageConsumer.csproj -c Release -r "$rid" -p:EasyNetQVersion="$version" -o out -nologo \
  --packages "$PWD/obj/nuget" 2>&1 | tee publish.log
warnings=$(grep -cE 'warning IL[23][0-9]{3}' publish.log || true)
echo "trim/AOT warnings: $warnings"
[ "$warnings" -eq 0 ] || exit 1

./out/PackageConsumer "${1:-${EASYNETQ_CONNECTION:-host=localhost}}"

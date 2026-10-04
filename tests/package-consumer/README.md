# Package consumer fixture

The Native AOT smoke sample (`Source/EasyNetQ.Examples.Aot/Program.cs`) compiled against the **packed** NuGet
packages instead of project references. `check.sh` packs the solution with a throwaway version, asserts that
`EasyNetQ.Core` ships the source generator (`analyzers/dotnet/cs`), publishes with `PublishAot`, fails on any
trim/AOT warning and runs the sample against a broker. A package that misses the generator or its
`InterceptorsNamespaces` opt-in (`buildTransitive/EasyNetQ.Core.props`) fails here, not in a consumer's build.
`publish-to-nuget` waits for this job.

On macOS the native link needs OpenSSL and brotli from Homebrew:
`LIBRARY_PATH=$(brew --prefix openssl@3)/lib:$(brew --prefix brotli)/lib tests/package-consumer/check.sh`.

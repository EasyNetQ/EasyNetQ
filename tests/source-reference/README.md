# Source-reference consumer fixture

An app consuming EasyNetQ from source the way `EasyNetQ.SourceReference.props` documents (e.g. a git submodule),
shaped like a real one: a Native AOT app, a second project that adds `EasyNetQ.Transport.InMemory` and references the
app, and a solution. `check.sh` builds it in Release and fails when an EasyNetQ project is built more than once or as
Debug: two instances of one project write the same `bin/obj` concurrently and break parallel builds intermittently.

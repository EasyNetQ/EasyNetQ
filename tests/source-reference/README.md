# Source-reference consumer fixture

An app consuming EasyNetQ from source the way `EasyNetQ.SourceReference.props` documents (e.g. a git submodule),
shaped like a real one: a Native AOT app, a second project that adds `EasyNetQ.Transport.InMemory` and references the
app, `App.Smoke` (references the app only, no import of the props), and a solution. `check.sh` builds the solution
and, separately, `App.Smoke` on its own (how a consuming app's CI builds its test project), both in Release, and fails
when an EasyNetQ project is built more than once or as Debug: two instances of one project write the same `bin/obj`
concurrently and break parallel builds intermittently.

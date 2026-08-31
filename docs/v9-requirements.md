# EasyNetQ v9 — requirements and guidelines

Working document for the v9 rewrite. Edit as decisions change; the phase plan derives from this.

## Original requirements

1. Config syntax somewhat backwards compatible; compat may live in a separate project.
2. Everything runs on a generic pipeline with a dictionary-like context base.
3. Middleware layers: connection -> channel/queue -> consumer -> handler. Lower layers cannot modify higher ones.
4. High-level methods stay compatible; every feature is a fine-grained pipeline step.
5. RabbitMQ isolated into a transport library.
6. No reflection in core.
7. Roslyn source generators for discovery/wiring.
8. Minimal allocations.
9. AOT as the last phase.
10. Benchmark every phase.

## Confirmed decisions

- TFMs: net8.0;net9.0;net10.0. netstandard2.0 dropped; 8.x branch serves .NET Framework users.
- Packages: EasyNetQ.Core + EasyNetQ.RabbitMQ; the EasyNetQ package id becomes the drop-in compat bundle.
- Compat: best effort. API shape kept, signatures may break; migration guide instead of an 8.x snapshot gate.
- Source generator: required for any assembly referencing Core.

## Guidelines added during the work

- Transport configurators own the lower layers: UseRabbitMq nests typed queue/channel/consumer/handler
  configurators. Top-level generic configurators remain only for portability and back-compat.
- Lifecycle pipelines replace the internal event bus (chosen over Mediator).
- Observability over logging: OpenTelemetry-conform tracing and metrics; the RabbitMQ client keeps the wire
  spans, EasyNetQ adds the semantic layer.
- Full RabbitMQ.Client 7.x alignment: do not build what the client already does (confirms tracking, recovery,
  callbacks, options).

## Delivery

- One branch and one stacked PR per phase; every phase commits results/phase{N}.md with benchmark deltas.
- Allocation ceilings only ratchet down; reflection ban and no-client-reference-in-Core are test-enforced.

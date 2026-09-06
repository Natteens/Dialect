# Dialect manual

Dialect separates Graph Toolkit authoring, compiled runtime data, per-play session state, and game UI. `.dlg` files always import a `DialectRuntimeGraph`; incomplete authoring remains referencable and carries diagnostics instead of producing a broken or missing main asset.

- [Getting started](getting-started.md)
- [Authoring](authoring.md)
- [Local and shared variables](variables.md)
- [Shared blackboards](shared-blackboards.md)
- [Localization](localization.md)
- [Runtime API](runtime.md)
- [Public API contracts](api.md)
- [Custom flow and value nodes](custom-nodes.md)
- [Validation](validation.md)
- [Runtime debugging](debugging.md)
- [Samples](samples.md)

Dialect requires Unity 6000.6 or newer and Unity Localization 1.5.9 or newer.

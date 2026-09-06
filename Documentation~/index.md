# Dialect manual

Dialect separates authoring, compiled runtime data, execution state, and UI. `.dlg` files are Graph Toolkit authoring assets. Their importer always emits a `DialectRuntimeGraph`, including when authoring is incomplete, so references remain stable and diagnostics remain visible without console spam.

Use the graph window for narrative flow, blackboard assets for shared defaults, a `DialectDirector` for playback, and ordinary game UI for presentation. Start with the [authoring guide](authoring.md), then integrate the [runtime API](runtime.md). Continue with [shared blackboards](shared-blackboards.md), [localization](localization.md), [validation](validation.md), and [debugging](debugging.md). Package authors can use the [custom-node SDK](custom-nodes.md).

Dialect requires Unity 6000.6+. Older `.dialect` assets and the pre-6000.4 Graph Toolkit compatibility layer are intentionally unsupported.

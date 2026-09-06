# Validation and importing

Graph Toolkit diagnostics report duplicate or missing Start nodes, missing End nodes, unreachable nodes, unconnected required outputs, and missing Action or Condition assets. Custom nodes add their own diagnostics through `DialectNodeValidationContext`.

The `.dlg` importer compiles semantic output names into explicit runtime indices. It always emits a `DialectRuntimeGraph`, even while authoring is incomplete. Invalid runtime assets keep their diagnostics and are rejected by `TryPlay`; `Play` includes those diagnostics in its exception.

New graphs are created as a connected Start to End flow, avoiding an invalid first import.

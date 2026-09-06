# Getting started and authoring

Create **Dialect > Dialogue Graph** from the Assets menu. The Project-window creation flow behaves like other Unity assets and creates a connected Start and End without opening a file browser.

Use Dialogue for a line that waits for `Advance`, Choice for explicit player targets, Condition for True/False routing, Action for project behavior, and End for completion. Flow outputs are single-capacity; End accepts multiple incoming connections. Text inputs accept authored `DialectText`, native Graph Toolkit string or `LocalizedString` variables, constants, Shared Variable nodes, and external compatible value nodes.

The graph toolbar provides:

- **Validate**: saves and reimports the graph so diagnostics reflect the compiled asset.
- **Shared Boards**: links, reorders, and removes `DialectBlackboard` assets with Undo.
- **Debug**: controls node, wire, and port preview visualization.

New graphs copy the optional boards configured in **Project Settings > Dialect** once. Changing the project default later does not mutate existing graphs.

An incomplete graph still imports. `DialectRuntimeGraph.IsValid` remains false and its `Diagnostics` explain why. This keeps Unity references stable during normal authoring.

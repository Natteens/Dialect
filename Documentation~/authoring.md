# Getting started and authoring

Create **Dialect > Dialogue Graph** from the Assets menu. The Project-window creation flow behaves like other Unity assets and creates a connected Start and End without opening a file browser.

Use Dialogue for a line that waits for `Advance`. Its Speaker and Line fields are edited directly inside the node: **Inline** is plain text and **Localized** is a String Table/entry picker. A connected value wire overrides the authored default while preserving it for later disconnection. Speaker is optional; Line is required by strict validation.

Choice presents the same inline/localized text authoring beside each numbered target. **+ Add** appends an option and **Remove Last** removes only the final option, preserving the meaning of all earlier connections. Branch routes a bool, Random Branch chooses one of N outputs using the Director's deterministic session seed, and Wait nodes suspend without changing the iterative execution core. Wait and Wait Until resume automatically; Wait For Resume continues only after consumer code calls `Resume`.

Set Variable assigns a local or shared session value. Modify Variable adds, subtracts, or multiplies numeric values, toggles Boolean values, and appends strings. Compare exposes only operators meaningful for its selected type, and Select chooses one of two typed values without duplicating flow nodes. Condition and Action use reusable ScriptableObject behavior.

The graph toolbar provides:

- **Validate**: saves and reimports the graph so diagnostics reflect the compiled asset.
- **Shared Boards**: links, reorders, and removes `DialectBlackboard` assets with Undo.
- **Runtime Debug**: visualizes a currently running session; it does not simulate dialogue in Edit Mode.

New graphs copy the optional boards configured in **Project Settings > Dialect** once. Changing the project default later does not mutate existing graphs.

Normal editing uses quiet live validation for structural corruption, duplicate Start nodes, invalid types, and broken references. Newly created Dialogue, Choice, Action, or Condition nodes are not punished with Console errors while they are being configured. **Validate**, import/compile, and playback use strict validation. An incomplete graph still imports as a stable `DialectRuntimeGraph`, with `IsValid == false` and actionable diagnostics.

Graph Toolkit already provides Sticky Notes, Placemats, and Groups for documentation and organization; Dialect does not duplicate them.

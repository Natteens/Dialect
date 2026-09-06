# Localization

`DialectText` stores an Inline, Localized, or Blackboard-authored fallback. A connected value can also resolve a string, `LocalizedString`, or `DialectText` at runtime. This keeps ports dynamic without adding dynamic cases to the serialized source enum.

Dialect uses synchronous `LocalizedString.GetLocalizedString()` when a node presents or refreshes. Dialogue presentation needs a value immediately, and Unity Localization has already loaded the selected locale in normal gameplay. Making the whole execution pump asynchronous would add state and allocation costs without improving this contract. Projects that stream tables late should preload those tables before starting a session.

While a Director is enabled it listens for `SelectedLocaleChanged`. If a line or choice is visible, only that node's presentation is resolved again. The node index, pending target, choices, and session identity remain unchanged. Repeated locale changes publish repeated replacement payloads; Stop, graph replacement, disable, and completion prevent stale refreshes.

Graph validation reports empty localized references, missing table collections, and missing entries. Runtime missing translations follow Unity Localization's configured fallback behavior and do not advance the graph.

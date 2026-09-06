# Authoring graphs

Create **Dialect > Dialogue Graph** from the Assets menu. New graphs contain a valid Start to End path. Add nodes from the Dialogue categories and connect every required flow output.

Dialogue uses a `DialectText` value for both speaker and line. Choose Inline, Localized, or Blackboard at the value source. Choice exposes one text value and one explicit target per option. Condition has named True and False outputs. Action and Condition nodes reference reusable ScriptableObjects.

Graph validation reports missing or duplicate Start nodes, missing End nodes, required outputs, missing actions or conditions, empty choices, and unreachable nodes in Graph Toolkit's diagnostics. An incomplete graph still imports as an invalid runtime asset with stored diagnostics.

Create shared defaults through **Dialect > Blackboard**. Variable IDs remain stable when renamed and are hidden from routine authoring. Runtime writes affect only the active session overlay.

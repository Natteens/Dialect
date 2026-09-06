# Getting started

Dialect requires Unity 6000.6 or newer. Install the package from its Git URL, then create a graph from **Assets > Create > Dialect > Dialogue Graph**. A new graph opens with a valid Start-to-End flow so it can be tested immediately.

Add Dialogue, Choice, Condition, and Action nodes from the graph node menu. Flow ports control execution; value ports can read inline values, local graph variables, linked shared blackboards, or values supplied by custom nodes.

Add `DialectDirector` to a GameObject and assign the imported `.dlg` runtime graph. Call `Play` to start a session, then use `Advance`, `Choose`, `Resume`, and `Stop` according to the current payload. Subscribe to the Director events to connect the runtime to UI.

For a complete working graph, import **Complete Dialogue Demo** from Package Manager. It includes localization, local and shared variables, a custom value node, a condition, an action, choices, and two graphs used by the same Director.

Continue with [Authoring](authoring.md), [Variables](variables.md), and [Runtime](runtime.md).

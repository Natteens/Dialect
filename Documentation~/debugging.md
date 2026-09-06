# Runtime debugging

In Play Mode, the `DialectDirector` Inspector shows the session state, current graph, current node ID, and valid playback controls. Open the active `.dlg` to see Graph Toolkit visualization pulse the current node. Visualization is cleared when the session ends, stops, is interrupted, or Play Mode exits.

Subscribe to `SessionFaulted` for runtime diagnostics and `SessionEnded` for the termination reason. `RunawayExecution` means an automatic flow exceeded `maxAutomaticSteps`; inspect the graph for a cycle without Dialogue, Choice, End, or a custom suspended node.

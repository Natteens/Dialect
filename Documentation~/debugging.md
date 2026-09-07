# Runtime debugging

The Director Inspector shows default configuration in Edit Mode and graph, state, node ID, termination, and only valid controls in Play Mode. **Open Active Graph** navigates to the `.dlg` asset.

With **Runtime Debug** enabled, Dialect creates a public Graph Visualization context for the active runtime graph. The current waiting/suspended node keeps a readable looping accent, only the most recently traversed wire animates, and resolved values appear as compact port previews.

The context is associated with the authoring graph model's stable `Graph.ID`, not the `.dlg` file GUID. Those identifiers are intentionally distinct in Graph Toolkit.

Visualization is state-driven. The session retains current node ID, last transition, and value previews. If Play Mode starts before the graph window finishes loading, if the graph is opened later, if Runtime Debug is enabled late, or if a Director appears after Play Mode starts, Dialect replays that state as soon as the public context reports `IsGraphLoaded`. A throttled readiness check exists only while sessions are active; it is removed when no state remains. End, Stop, interruption, fault, disable/destroy, graph replacement, and Play Mode exit dispose the owned context and clear visuals.

Graph Toolkit 6.6's public `IGraphWindow` exposes only `Graph`. It has no public node selection/framing method, and its internal window implementation is unsupported. Dialect therefore opens the active graph but does not implement a reflection-based Frame Current command. The same public limitation prevents reliable framing from an Unreachable diagnostic action.

Graph Toolkit exposes public graph-level subgraph creation and `ISubgraphNode.GetSubgraph`, but no public contract for mapping Dialect entry/exit flow ports to subgraph boundaries. Dialect leaves executable subgraphs out rather than depending on serialized or internal implementation details.

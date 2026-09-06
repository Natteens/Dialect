# Runtime debugging

The Director Inspector shows default configuration in Edit Mode and graph, state, node ID, termination, and only valid controls in Play Mode. **Open Active Graph** navigates to the `.dlg` asset.

With the graph toolbar **Debug** toggle enabled, the open graph highlights the current node, animates only the traversed wire, and previews compact resolved data values. Directors already in the scene and Directors enabled or created after Play Mode starts are tracked through lifecycle events. End, Stop, interruption, fault, disable/destroy, graph replacement, and Play Mode exit clear subscriptions and visualization.

Graph Toolkit 6.6's public `IGraphWindow` exposes only `Graph`. It has no public node selection/framing method, and its internal window implementation is unsupported. Dialect therefore opens the active graph but does not implement a reflection-based Frame Current command. The same public limitation prevents reliable framing from an Unreachable diagnostic action.

Graph Toolkit exposes public graph-level subgraph creation and `ISubgraphNode.GetSubgraph`, but no public contract for mapping Dialect entry/exit flow ports to subgraph boundaries. Dialect leaves executable subgraphs out rather than depending on serialized or internal implementation details.

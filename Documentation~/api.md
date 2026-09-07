# Public API contracts

Use `DialectDirector` and `DialectRuntimeGraph` from game code. Treat `DialectSession` as read-only playback state; mutate dialogue values through its `DialectVariableStore` methods.

`DialectBlackboard.Variables` and runtime graph collections are read-only views. Author shared variables through the Inspector or `AddVariable`, `DuplicateVariable`, `MoveVariable`, and `RemoveVariable`. `DialectGraph.LinkBlackboard` and `UnlinkBlackboard` are Editor API for external authoring tools.

`DialectExecutionResult` is the explicit result of a runtime node: continue, wait for advance, await choices, suspend, or end. `Suspended()` preserves the legacy custom-node contract and re-executes that node after `Resume`. `SuspendTo(target)` represents a manual suspension that continues to a target after `Resume`; automatic built-ins use a separate internal suspension mode. Runtime nodes must return a valid semantic target supplied by their compiled authoring node.

`DialectLine` and `DialectChoiceSet` are presentation payloads. `DialectTransition` maps compiled source/target indices to Graph Toolkit port IDs for visualization. `DialectValuePreview` carries a compact resolved value for a data port.

`DialectAction` and `DialectCondition` remain the smallest reusable project integration points. For new flow semantics derive a serializable `RuntimeNode`; for computed inputs derive `DialectValueResolver`. Keep Editor node types in Editor assemblies.

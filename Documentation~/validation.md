# Validation and importing

GraphLogger reports zero/multiple Start, zero End, unreachable flow nodes, missing required outputs, empty choices, missing Action/Condition assets, invalid text/value connections, missing or unlinked shared variables, variable ID/name conflicts, missing localization tables/entries, custom compiler failures, and detectable automatic-only cycles.

Safe actions are attached to missing Start and missing End diagnostics. They only add the absent structural node. Blackboard diagnostics offer Select Blackboard. Dialect does not delete nodes or invent narrative connections from a quick fix.

The importer compiles semantic port names to explicit node indices and records port IDs for runtime visualization. It catches custom compiler exceptions and substitutes an invalid runtime node while preserving the main imported asset. `TryPlay` rejects any runtime asset with diagnostics.

Only flow nodes participate in reachability warnings. Value and native variable nodes are dependencies rather than flow destinations, so they are not incorrectly reported as unreachable.

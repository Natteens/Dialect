# Shared blackboards

Create a `DialectBlackboard` from **Assets > Create > Dialect > Blackboard**. Each variable has a stable hidden ID, a readable name, a type, and an asset default. Renaming or reordering a variable preserves references.

Link one or more boards from the graph settings. A `DialectSession` clones their defaults into its own `DialectVariableStore`, so dialogue can override values without modifying project assets or another active session. String and `LocalizedString` variables can feed a line or speaker through `DialectText.Blackboard`.

Use `CreateSnapshot` and `RestoreSnapshot` when adapting session values to a save system. The snapshot API is package-agnostic and does not prescribe storage or serialization.

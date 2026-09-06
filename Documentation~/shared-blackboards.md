# Shared blackboards

Create **Dialect > Blackboard** from the Assets menu. The Inspector supports Add by type, remove, rename, reorder, duplicate, search by name/type, Undo/Redo through serialized editing, and validation. Stable IDs are deliberately hidden from routine authoring.

Link boards with **Shared Boards** in a `.dlg` toolbar. A Shared Variable node must reference a variable from a board linked to that graph; otherwise authoring and import validation report the missing link before runtime.

Use **Project Settings > Dialect > Default Shared Blackboards** for optional project conventions. These settings are Editor-only and apply only when a new graph is created. Runtime has no project-settings singleton.

Multiple sessions get independent copies of the defaults. Use `CreateSnapshot` and `RestoreSnapshot` in a save adapter when dialogue variables belong in persistence.

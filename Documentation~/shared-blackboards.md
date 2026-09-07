# Shared blackboards

Create **Dialect > Blackboard** from the Assets menu. Every row explicitly shows **Name**, **Type**, and **Default Value**. Name is the readable authoring identity; Default Value is copied into each new session. The Inspector supports Add by type, remove, rename, reorder, duplicate, real search filtering, Undo/Redo, and validation. Stable IDs are deliberately hidden and survive rename/reorder.

Link boards with **Shared Boards** in a `.dlg` toolbar. The window provides Add, Remove, reorder, Open, duplicate prevention, and Create Blackboard. A Shared Variable node then shows one combined Variable picker. With one board it displays `Player Name`; with several it displays paths such as `GlobalDialogue / Player Name`. If a board is unlinked, the stored stable reference remains repairable and strict validation reports it.

Use **Project Settings > Dialect > Default Shared Blackboards** for optional project conventions. These settings are Editor-only and apply only when a new graph is created. Runtime has no project-settings singleton.

Multiple sessions get independent copies of the defaults. Use `CreateSnapshot` and `RestoreSnapshot` in a save adapter when dialogue variables belong in persistence.

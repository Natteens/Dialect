# Runtime UI sample

This adapter demonstrates how a game-owned UI can consume `DialectDirector` events without the package imposing a presentation layer.

Create a runtime UI Toolkit document with labels named `speaker` and `line`, assign it and a director to `DialectRuntimeUI`, then connect UI buttons to `Advance` and `Choose`. The same director can receive different `.dlg` runtime assets through `Play`.

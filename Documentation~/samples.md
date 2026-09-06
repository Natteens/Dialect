# Samples

## Runtime UI

A minimal event adapter showing how ordinary UI code consumes line and choice payloads.

## Complete Dialogue Demo

Import the sample, then run **Tools > Dialect > Create Complete Demo Assets**. The setup creates editable Greeting and Goodbye graphs, a shared blackboard, an Action, a Condition, and a localized string table for the project's configured locales.

Greeting demonstrates Dialogue, Choice, Condition, Action, local and shared values, a custom computed value node, localization, a session override, and End. Goodbye demonstrates a second graph on the same Director. The included UI Toolkit UXML/USS and `DialectCompleteDemo` component provide the presentation adapter.

The setup is intentionally a menu action: imported samples cannot safely assume a project's locales, asset destination, or scene. It creates ordinary assets once and refuses to overwrite an existing demo folder.

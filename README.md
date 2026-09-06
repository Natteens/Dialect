# Dialect

Dialect is a typed dialogue graph runtime and authoring toolkit for Unity 6.6. It keeps the graph focused on narrative flow while letting game code own presentation, input, actions, conditions, and save data.

## Requirements

- Unity 6000.6 or newer
- Unity Localization 1.5.9 or newer

Graph Toolkit ships as a Unity module in 6000.6, so Dialect does not declare the obsolete experimental package.

## Install

Add the Git URL through Package Manager:

```
https://github.com/Natteens/Dialect.git
```

## First dialogue

1. Create a graph with **Assets > Create > Dialect > Dialogue Graph**. A connected Start and End are created automatically.
2. Add Dialogue, Choice, Condition, or Action nodes from the graph library.
3. Add `DialectDirector` to a scene object and assign the imported `.dlg` runtime asset as its default graph.
4. Subscribe to typed events and call `Advance` or `Choose` from your UI.

```csharp
director.LinePresented += line => view.Show(line.Speaker, line.Text);
director.ChoicesPresented += choices => view.Show(choices.Choices);
director.SessionEnded += (_, reason) => view.Hide();

director.Play(greetingGraph, playerContext);
director.Advance();
director.Choose(0);
```

One director can play any number of graph assets. `TryPlay` returns false for an invalid graph; `Play` reports invalid requests with an exception. Starting another graph interrupts the active session explicitly.

## Runtime model

`DialectSession` owns the current graph, node, playback state, termination reason, user data, choices, and a per-session variable overlay. The director executes automatic nodes in an iterative pump with a configurable runaway guard. Runtime assets and blackboard assets are never mutated.

Nodes return `DialectExecutionResult`: continue to a target, wait for advance, await a choice, suspend, or end. Choice and condition targets are compiled by semantic port name rather than visual port order.

## Localization and variables

Dialogue text and speakers accept inline text, a `LocalizedString`, or a shared blackboard variable. Locale changes refresh the visible line or choices without advancing the session. Create reusable boards with **Assets > Create > Dialect > Blackboard**; their Inspector supports typed defaults, reorder, duplicate, search, and validation while hiding stable IDs.

Supported values are string, localized string, bool, int, float, and Unity Object. Each play session copies defaults into a runtime overlay.

## Extending Dialect

Authoring extensions live in an Editor assembly. Derive from public `DialectNode`, implement `IDialectNodeCompiler`, and return a serializable `RuntimeNode`. The importer discovers the interface and does not contain a switch over built-in node types. Reusable `DialectAction` and `DialectCondition` assets remain the quickest option for project logic.

See [the manual](Documentation~/index.md), [runtime API](Documentation~/runtime.md), [authoring guide](Documentation~/authoring.md), [shared blackboards](Documentation~/shared-blackboards.md), [localization](Documentation~/localization.md), [validation](Documentation~/validation.md), [debugging](Documentation~/debugging.md), and [custom-node guide](Documentation~/custom-nodes.md).

## License

[MIT](LICENSE.md)

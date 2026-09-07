# Dialect

Dialect is a typed dialogue graph runtime and authoring toolkit for Unity 6.6. Graph Toolkit owns visual authoring; compact compiled assets and an iterative runtime own playback. Game code keeps control of presentation, input, actions, conditions, and persistence.

## Requirements

- Unity 6000.6 or newer
- Unity Localization 1.5.9 or newer

Graph Toolkit ships with Unity 6000.6, so Dialect does not declare an old experimental Graph Toolkit package.

## Features

- `.dlg` graphs with Start, Dialogue, Choice, Branch, Random Branch, Condition, Action, Set Variable, and End
- local Graph Toolkit variables and reusable `DialectBlackboard` assets
- string, `LocalizedString`, bool, int, float, and Unity Object values
- inline, localized, local, shared, constant, and custom connected typed values
- per-session overrides, snapshots, and restore without mutating assets
- public custom flow and value-node SDKs
- direct Inline/Localized Dialogue and Choice authoring through a UI Toolkit drawer
- quiet live structural diagnostics plus explicit strict validation
- Validate, Shared Boards, and Runtime Debug graph toolbar controls
- state-replayed current-node, traversed-wire, and compact port-value visualization
- a Play Mode Director Inspector and a complete UI Toolkit sample

## Installation

Add this Git URL through Unity Package Manager:

```
https://github.com/Natteens/Dialect.git
```

## Quick start

1. Create **Assets > Create > Dialect > Dialogue Graph**. Dialect creates a connected Start and End.
2. Insert a Dialogue, type Speaker and Line directly in its Inline fields, and connect the flow.
3. Add `DialectDirector` to a scene object and assign the imported `.dlg` asset.
4. Subscribe a UI adapter and call `Advance`, `Choose`, or `Resume` only when valid.

```csharp
director.LinePresented += line => view.Show(line.Speaker, line.Text);
director.ChoicesPresented += choices => view.Show(choices.Choices);
director.SessionEnded += (_, reason) => view.Hide();

director.Play(greetingGraph, playerContext);
director.Advance();
director.Choose(0);
```

`TryPlay` returns `false` for invalid requests. `Play` throws with compile diagnostics. A successful new Play interrupts the old session; an invalid replacement leaves the old session running.

## Variables and localization

Local variables live inside one `.dlg`. Shared values live in `DialectBlackboard` assets. A session builds its store in this order: local defaults, shared defaults, then caller overrides. Conflicting IDs or names are rejected by authoring/import validation; duplicate IDs are also rejected at the runtime boundary.

Dialogue and Choice text refresh when the selected locale changes. Refreshing republishes the visible payload without moving the current node or restarting the session.

## Extensibility and debugging

External Editor assemblies can derive `DialectNode` or `DialectValueNode` and implement the matching compiler interface. Runtime assemblies provide serializable `RuntimeNode` and `DialectValueResolver` implementations. The importer discovers these contracts without changes to Dialect.

During Play Mode, select the Director for valid playback controls or open its active graph to see runtime visualization. See the [manual](Documentation~/index.md), [API guide](Documentation~/api.md), [custom-node guide](Documentation~/custom-nodes.md), and [samples](Documentation~/samples.md).

## License

[MIT](LICENSE.md)

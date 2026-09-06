# Local and shared variables

Local variables use Graph Toolkit's built-in blackboard and live inside one `.dlg`. Shared variables use a `DialectBlackboard` asset and can be linked to many graphs.

At Play time `DialectVariableStore` copies values into a session-owned overlay in this order:

1. local defaults;
2. linked shared defaults;
3. caller overrides passed to `Play` or `TryPlay`.

Assets are never mutated. IDs remain stable through rename and reorder. Duplicating a shared variable creates a new ID and clones its value. The importer rejects empty or duplicate IDs, duplicate names, repeated board references, and conflicts across local/shared scopes. The runtime constructor repeats the ID and repeated-board checks for graphs constructed outside the importer.

Supported built-ins are string, `LocalizedString`, bool, int, float, and `UnityEngine.Object`. Native local string and localized variables can connect directly to speaker or text inputs. Use a Shared Variable node for a shared string or localized value.

```csharp
var overrides = new Dictionary<string, DialectValue>();
if (graph.TryGetVariableDefinition("PlayerName", out var variable))
    overrides.Add(variable.Id, new DialectStringValue("Mara"));
director.Play(graph, playerContext, overrides);
```

`CreateSnapshot` clones the session values. `RestoreSnapshot` applies compatible known values and leaves the graph and board assets unchanged.

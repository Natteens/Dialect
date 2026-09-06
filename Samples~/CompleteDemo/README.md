# Complete Dialect demo

This sample contains a UI Toolkit adapter, reusable Action and Condition assets, session variable overrides, and an external custom value node.

Run **Tools > Dialect > Create Complete Demo Assets** after importing the sample. It creates `Greeting.dlg`, `Goodbye.dlg`, a shared blackboard, Action and Condition assets, and a localized welcome table for the locales already configured in the project. `Greeting` contains Start, Dialogue, Choice, Condition, Action, local/shared variables, a connected custom value node, and End. `Goodbye` proves that the same Director can switch graphs.

Create a scene object with `DialectDirector`, `SampleState`, and `DialectCompleteDemo`. Add a `UIDocument` using `DialogueView.uxml`, then assign the generated graphs and component references. The graph assets remain ordinary editable Graph Toolkit assets; the setup action only removes repetitive sample setup.

Pass a `SampleState` as user data when the graph needs `IsAcceptedCondition`, `SetAcceptedAction`, or `SamplePlayerNameResolver`. The same Director can play both graphs. `PlayGreeting` demonstrates a session override without modifying graph or blackboard assets.

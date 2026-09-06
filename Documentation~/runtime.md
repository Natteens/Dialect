# Runtime API

Call `Play(graph, userData)` when failure is exceptional or `TryPlay` when a missing/invalid graph is an expected branch. `Stop`, `Advance`, `Choose`, and `Resume` return or expose enough state for UI code to reject invalid input without ending dialogue.

`DialectSession.State` distinguishes running, waiting for advance, waiting for choice, suspended, ended, and faulted. `TerminationReason` distinguishes completion, stop, interruption, disable, invalid data, execution errors, and runaway automatic execution.

`DialectExecutionContext` exposes the active Director, Graph, Session, Variables, and typed access to the optional `UserData`. Treat UserData as an integration escape hatch; prefer typed actions, conditions, and blackboard values for reusable logic.

The execution pump is iterative. A graph made only of automatic nodes is stopped by `maxAutomaticSteps` instead of overflowing the stack. Event callbacks may request advance or choose while the pump is dispatching; the director queues the command and applies it after the node result.

Selected-locale changes rerun only the current node's presentation refresh hook. Built-in Dialogue and Choice nodes update their typed payload without changing the current node.

# Runtime API

`DialectDirector` owns one active `DialectSession`. `Play` is for requests that must succeed; `TryPlay` is for expected invalid input. Both accept an optional user-data object, and overloads accept a read-only dictionary of variable overrides. Validation and overrides are completed before an active session is interrupted.

Playback commands are state-aware:

- `Advance` succeeds only for `WaitingForAdvance`.
- `Choose(index)` succeeds only for a valid current choice.
- `Resume` succeeds only for `Suspended`.
- `Stop` ends an active session with `Stopped`.

Callbacks may call Advance, Choose, or Stop reentrantly while a payload is being dispatched. The iterative pump queues the valid command and applies it after the current runtime result. Automatic loops fault at `maxAutomaticSteps` instead of overflowing the stack.

`DialectSession` exposes Graph, Variables, UserData, current node index/ID, state, termination reason, current line, and current choices. Runtime nodes receive `DialectExecutionContext`, which exposes the same typed integration surface plus `TryGetUserData<T>`.

The main events are `SessionStarted`, `NodeEntered`, `Transitioned`, `ValueResolved`, `LinePresented`, `ChoicesPresented`, `SessionFaulted`, and `SessionEnded`. Termination distinguishes Completed, Stopped, Interrupted, DirectorDisabled, InvalidGraph, ExecutionError, and RunawayExecution.

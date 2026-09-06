using Dialect.Blackboards;

namespace Dialect.Core
{
    public enum DialectPlaybackState
    {
        Idle,
        Running,
        WaitingForAdvance,
        WaitingForChoice,
        Suspended,
        Ended,
        Faulted
    }

    public enum DialectTerminationReason
    {
        Completed,
        Stopped,
        Interrupted,
        DirectorDisabled,
        InvalidGraph,
        ExecutionError,
        RunawayExecution
    }

    public sealed class DialectSession
    {
        internal DialectSession(DialectRuntimeGraph graph, DialectVariableStore variables, object userData)
        {
            Graph = graph;
            Variables = variables;
            UserData = userData;
            CurrentNodeIndex = graph.EntryNodeIndex;
            State = DialectPlaybackState.Running;
        }

        public DialectRuntimeGraph Graph { get; }
        public DialectVariableStore Variables { get; }
        public object UserData { get; }
        public int CurrentNodeIndex { get; internal set; }
        public string CurrentNodeId { get; internal set; }
        public DialectPlaybackState State { get; internal set; }
        public DialectTerminationReason? TerminationReason { get; internal set; }
        public DialectLine CurrentLine { get; internal set; }
        public DialectChoiceSet CurrentChoices { get; internal set; }
        internal int PendingTarget { get; set; } = -1;
    }
}

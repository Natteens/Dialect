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
        readonly System.Random random;
        readonly System.Collections.Generic.Dictionary<string, string> valuePreviews = new();

        internal DialectSession(DialectRuntimeGraph graph, DialectVariableStore variables, object userData, int randomSeed)
        {
            Graph = graph;
            Variables = variables;
            UserData = userData;
            CurrentNodeIndex = graph.EntryNodeIndex;
            State = DialectPlaybackState.Running;
            RandomSeed = randomSeed;
            random = new System.Random(randomSeed);
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
        public DialectTransition? LastTransition { get; internal set; }
        public System.Collections.Generic.IReadOnlyDictionary<string, string> ValuePreviews => valuePreviews;
        public int RandomSeed { get; }
        internal int PendingTarget { get; set; } = -1;
        internal DialectSuspensionKind SuspensionKind { get; set; }
        internal int NextRandom(int maximum) => random.Next(maximum);
        internal bool SetValuePreview(string portId, string value)
        {
            if (string.IsNullOrEmpty(portId)) return false;
            value ??= string.Empty;
            if (valuePreviews.TryGetValue(portId, out var current) && current == value) return false;
            valuePreviews[portId] = value;
            return true;
        }
    }
}

using System.Collections.Generic;

namespace Dialect.Blackboards
{
    public sealed class DialectVariableStore
    {
        readonly Dictionary<string, DialectValue> values = new();
        public DialectVariableStore(IEnumerable<DialectBlackboard> blackboards)
            : this(null, blackboards) { }

        public DialectVariableStore(IEnumerable<DialectVariableDefinition> localVariables,
            IEnumerable<DialectBlackboard> sharedBlackboards)
        {
            AddDefaults(localVariables, "local variables");
            if (sharedBlackboards == null) return;
            var boards = new HashSet<DialectBlackboard>();
            foreach (var blackboard in sharedBlackboards)
            {
                if (blackboard == null) continue;
                if (!boards.Add(blackboard))
                    throw new System.InvalidOperationException($"Blackboard '{blackboard.name}' is referenced more than once.");
                AddDefaults(blackboard.Variables, $"blackboard '{blackboard.name}'");
            }
        }
        public int Count => values.Count;
        public bool Contains(string id) => !string.IsNullOrWhiteSpace(id) && values.ContainsKey(id);
        public bool TryGetValue(string id, out DialectValue value) => values.TryGetValue(id, out value);
        public bool TryGet<T>(string id, out T value)
        {
            if (values.TryGetValue(id, out var stored) && stored.BoxedValue is T typed)
            { value = typed; return true; }
            value = default; return false;
        }
        public bool TrySet(string id, DialectValue value)
        {
            if (value == null || !values.TryGetValue(id, out var current) || current.Type != value.Type) return false;
            values[id] = value.Clone(); return true;
        }

        public IReadOnlyDictionary<string, DialectValue> CreateSnapshot()
        {
            var snapshot = new Dictionary<string, DialectValue>(values.Count);
            foreach (var pair in values) snapshot.Add(pair.Key, pair.Value.Clone());
            return snapshot;
        }

        public void RestoreSnapshot(IReadOnlyDictionary<string, DialectValue> snapshot)
        {
            if (snapshot == null) return;
            foreach (var pair in snapshot) TrySet(pair.Key, pair.Value);
        }

        void AddDefaults(IEnumerable<DialectVariableDefinition> definitions, string source)
        {
            if (definitions == null) return;
            foreach (var definition in definitions)
            {
                if (definition?.DefaultValue == null || string.IsNullOrWhiteSpace(definition.Id))
                    throw new System.InvalidOperationException($"{source} contains an invalid variable definition.");
                if (!values.TryAdd(definition.Id, definition.DefaultValue.Clone()))
                    throw new System.InvalidOperationException($"Variable ID '{definition.Id}' is duplicated across {source}.");
            }
        }
    }
}

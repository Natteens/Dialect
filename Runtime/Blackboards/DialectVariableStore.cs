using System.Collections.Generic;

namespace Dialect.Blackboards
{
    public sealed class DialectVariableStore
    {
        readonly Dictionary<string, DialectValue> values = new();
        public DialectVariableStore(IEnumerable<DialectBlackboard> blackboards)
        {
            if (blackboards == null) return;
            foreach (var blackboard in blackboards)
            {
                if (blackboard == null) continue;
                foreach (var definition in blackboard.Variables)
                    if (definition?.DefaultValue != null) values[definition.Id] = definition.DefaultValue.Clone();
            }
        }
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
    }
}

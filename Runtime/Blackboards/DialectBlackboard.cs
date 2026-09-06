using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dialect.Blackboards
{
    [Serializable]
    public sealed class DialectVariableDefinition
    {
        [SerializeField] string id = Guid.NewGuid().ToString("N");
        [SerializeField] string name = "Variable";
        [SerializeReference] DialectValue defaultValue = new DialectStringValue(string.Empty);
        public DialectVariableDefinition() { }
        public DialectVariableDefinition(string name, DialectValue defaultValue)
        {
            this.name = string.IsNullOrWhiteSpace(name) ? "Variable" : name;
            this.defaultValue = defaultValue ?? new DialectStringValue(string.Empty);
        }
        internal DialectVariableDefinition(string id, string name, DialectValue defaultValue)
        {
            this.id = id;
            this.name = string.IsNullOrWhiteSpace(name) ? "Variable" : name;
            this.defaultValue = defaultValue ?? new DialectStringValue(string.Empty);
        }
        public string Id => id;
        public string Name { get => name; set => name = value; }
        public DialectValue DefaultValue { get => defaultValue; set => defaultValue = value; }
        public DialectValueType Type => defaultValue?.Type ?? DialectValueType.String;
        public DialectVariableDefinition Duplicate() => new()
        { id = Guid.NewGuid().ToString("N"), name = name + " Copy", defaultValue = defaultValue?.Clone() };
        internal void RegenerateId() => id = Guid.NewGuid().ToString("N");
    }

    [CreateAssetMenu(menuName = "Dialect/Blackboard", fileName = "DialectBlackboard")]
    public sealed class DialectBlackboard : ScriptableObject
    {
        [SerializeField] List<DialectVariableDefinition> variables = new();
        public IReadOnlyList<DialectVariableDefinition> Variables => variables;

        public DialectVariableDefinition AddVariable(string name, DialectValue defaultValue)
        {
            var definition = new DialectVariableDefinition(name, defaultValue);
            variables.Add(definition);
            return definition;
        }

        public bool RemoveVariable(string id) => variables.RemoveAll(item => item != null && item.Id == id) > 0;

        public DialectVariableDefinition DuplicateVariable(string id)
        {
            var index = variables.FindIndex(item => item != null && item.Id == id);
            if (index < 0) return null;
            var copy = variables[index].Duplicate();
            variables.Insert(index + 1, copy);
            return copy;
        }

        public bool MoveVariable(string id, int destinationIndex)
        {
            var index = variables.FindIndex(item => item != null && item.Id == id);
            if (index < 0 || destinationIndex < 0 || destinationIndex >= variables.Count || index == destinationIndex) return false;
            var variable = variables[index];
            variables.RemoveAt(index);
            variables.Insert(destinationIndex, variable);
            return true;
        }

        public bool TryGetDefinition(string id, out DialectVariableDefinition definition)
        {
            definition = variables.Find(item => item != null && item.Id == id);
            return definition != null;
        }

        public bool TryGetDefinitionByName(string variableName, out DialectVariableDefinition definition)
        {
            definition = variables.Find(item => item != null && string.Equals(item.Name, variableName, StringComparison.OrdinalIgnoreCase));
            return definition != null;
        }

        public void GetDiagnostics(ICollection<string> diagnostics)
        {
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < variables.Count; i++)
            {
                var variable = variables[i];
                if (variable == null) { diagnostics.Add($"Variable {i + 1} is missing."); continue; }
                if (string.IsNullOrWhiteSpace(variable.Id)) diagnostics.Add($"Variable '{variable.Name}' has an empty ID.");
                else if (!ids.Add(variable.Id)) diagnostics.Add($"Variable ID '{variable.Id}' is duplicated.");
                if (string.IsNullOrWhiteSpace(variable.Name)) diagnostics.Add($"Variable {i + 1} has an empty name.");
                else if (!names.Add(variable.Name)) diagnostics.Add($"Variable name '{variable.Name}' is duplicated.");
                if (variable.DefaultValue == null) diagnostics.Add($"Variable '{variable.Name}' has no default value.");
            }
        }

        internal bool RepairVariableIds()
        {
            var changed = false;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var variable in variables)
            {
                if (variable == null) continue;
                if (!string.IsNullOrWhiteSpace(variable.Id) && ids.Add(variable.Id)) continue;
                do variable.RegenerateId(); while (!ids.Add(variable.Id));
                changed = true;
            }
            return changed;
        }

        void OnValidate() => RepairVariableIds();
    }

    [Serializable]
    public struct DialectVariableReference
    {
        [SerializeField] DialectBlackboard blackboard;
        [SerializeField] string variableId;
        public DialectBlackboard Blackboard => blackboard;
        public string VariableId => variableId;
        public bool IsValid => blackboard != null && blackboard.TryGetDefinition(variableId, out _);
        public DialectVariableReference(DialectBlackboard blackboard, string variableId)
        { this.blackboard = blackboard; this.variableId = variableId; }
    }
}

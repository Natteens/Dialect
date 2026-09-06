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
        public string Id => id;
        public string Name { get => name; set => name = value; }
        public DialectValue DefaultValue { get => defaultValue; set => defaultValue = value; }
        public DialectValueType Type => defaultValue?.Type ?? DialectValueType.String;
        public DialectVariableDefinition Duplicate() => new()
        { id = Guid.NewGuid().ToString("N"), name = name + " Copy", defaultValue = defaultValue?.Clone() };
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

        public bool TryGetDefinition(string id, out DialectVariableDefinition definition)
        {
            definition = variables.Find(item => item != null && item.Id == id);
            return definition != null;
        }
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

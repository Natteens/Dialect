using System.Collections.Generic;
using Dialect.Blackboards;
using UnityEngine;

namespace Dialect.Core
{
    public class DialectRuntimeGraph : ScriptableObject
    {
        [SerializeField] string graphId;
        [SerializeField] bool isValid;
        [SerializeField] int entryNodeIndex = -1;
        [SerializeField] List<DialectBlackboard> blackboards = new();
        [SerializeField] List<DialectVariableDefinition> localVariables = new();
        [SerializeField] List<DialectTransition> transitions = new();
        [SerializeField] List<string> diagnostics = new();
        [SerializeReference] List<RuntimeNode> nodes = new();

        public string GraphId => graphId;
        public bool IsValid => isValid;
        public int EntryNodeIndex => entryNodeIndex;
        public IReadOnlyList<DialectBlackboard> Blackboards => blackboards;
        public IReadOnlyList<DialectVariableDefinition> LocalVariables => localVariables;
        public IReadOnlyList<DialectTransition> Transitions => transitions;
        public IReadOnlyList<string> Diagnostics => diagnostics;
        public IReadOnlyList<RuntimeNode> Nodes => nodes;

        public bool TryGetNode(int index, out RuntimeNode node)
        {
            if (index >= 0 && index < nodes.Count)
            {
                node = nodes[index];
                return node != null;
            }

            node = null;
            return false;
        }

        public void Configure(string id, int entryIndex, List<RuntimeNode> compiledNodes,
            List<DialectBlackboard> linkedBlackboards, List<string> compileDiagnostics)
            => Configure(id, entryIndex, compiledNodes, null, linkedBlackboards, null, compileDiagnostics);

        public void Configure(string id, int entryIndex, List<RuntimeNode> compiledNodes,
            List<DialectVariableDefinition> graphVariables, List<DialectBlackboard> linkedBlackboards,
            List<DialectTransition> compiledTransitions, List<string> compileDiagnostics)
        {
            graphId = id;
            entryNodeIndex = entryIndex;
            nodes = compiledNodes ?? new List<RuntimeNode>();
            blackboards = linkedBlackboards ?? new List<DialectBlackboard>();
            localVariables = graphVariables ?? new List<DialectVariableDefinition>();
            transitions = compiledTransitions ?? new List<DialectTransition>();
            diagnostics = compileDiagnostics ?? new List<string>();
            isValid = entryNodeIndex >= 0 && entryNodeIndex < nodes.Count && diagnostics.Count == 0;
        }

        public bool TryGetVariableDefinition(string name, out DialectVariableDefinition definition)
        {
            definition = null;
            if (string.IsNullOrWhiteSpace(name)) return false;
            for (var i = 0; i < localVariables.Count; i++)
            {
                var candidate = localVariables[i];
                if (candidate != null && string.Equals(candidate.Name, name, System.StringComparison.OrdinalIgnoreCase))
                { definition = candidate; return true; }
            }
            for (var boardIndex = 0; boardIndex < blackboards.Count; boardIndex++)
                if (blackboards[boardIndex] != null && blackboards[boardIndex].TryGetDefinitionByName(name, out definition)) return true;
            return false;
        }

        public bool TryGetTransition(int from, int to, out DialectTransition transition)
        {
            for (var i = 0; i < transitions.Count; i++)
                if (transitions[i].FromNodeIndex == from && transitions[i].TargetNodeIndex == to)
                { transition = transitions[i]; return true; }
            transition = default;
            return false;
        }
    }

    [System.Serializable]
    public struct DialectTransition
    {
        [SerializeField] int fromNodeIndex;
        [SerializeField] int targetNodeIndex;
        [SerializeField] string outputPortId;
        [SerializeField] string inputPortId;

        public DialectTransition(int from, int target, string outputPort, string inputPort)
        { fromNodeIndex = from; targetNodeIndex = target; outputPortId = outputPort; inputPortId = inputPort; }
        public int FromNodeIndex => fromNodeIndex;
        public int TargetNodeIndex => targetNodeIndex;
        public string OutputPortId => outputPortId;
        public string InputPortId => inputPortId;
    }
}

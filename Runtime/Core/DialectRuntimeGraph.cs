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
        [SerializeField] List<string> diagnostics = new();
        [SerializeReference] List<RuntimeNode> nodes = new();

        public string GraphId => graphId;
        public bool IsValid => isValid;
        public int EntryNodeIndex => entryNodeIndex;
        public IReadOnlyList<DialectBlackboard> Blackboards => blackboards;
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
        {
            graphId = id;
            entryNodeIndex = entryIndex;
            nodes = compiledNodes ?? new List<RuntimeNode>();
            blackboards = linkedBlackboards ?? new List<DialectBlackboard>();
            diagnostics = compileDiagnostics ?? new List<string>();
            isValid = entryNodeIndex >= 0 && entryNodeIndex < nodes.Count && diagnostics.Count == 0;
        }
    }
}

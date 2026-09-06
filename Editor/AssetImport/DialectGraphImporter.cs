using System.Collections.Generic;
using System.Linq;
using Dialect.Blackboards;
using Dialect.Core;
using Dialect.Editor.Nodes;
using Unity.GraphToolkit.Editor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Dialect.Editor.AssetImport
{
    [ScriptedImporter(2, DialectGraph.AssetExtension)]
    public sealed class DialectGraphImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext context)
        {
            var runtime = ScriptableObject.CreateInstance<DialectRuntimeGraph>();
            runtime.name = System.IO.Path.GetFileNameWithoutExtension(context.assetPath);
            var diagnostics = new List<string>();
            var graph = GraphDatabase.LoadGraphForImporter<DialectGraph>(context.assetPath);
            if (graph == null)
            {
                diagnostics.Add("The authoring graph could not be loaded.");
                runtime.Configure(context.assetPath, -1, new List<RuntimeNode>(), null, diagnostics);
                AddRuntime(context, runtime);
                return;
            }

            var authoringNodes = graph.GetNodes().OfType<DialectNode>().ToList();
            var localVariables = CompileLocalVariables(graph, diagnostics);
            var blackboards = ValidateBlackboards(graph.Blackboards, localVariables, diagnostics);
            var indices = new Dictionary<INode, int>();
            for (var i = 0; i < authoringNodes.Count; i++) indices[authoringNodes[i]] = i;
            var compiled = new List<RuntimeNode>(authoringNodes.Count);
            foreach (var node in authoringNodes)
            {
                RuntimeNode runtimeNode = null;
                if (node is IDialectNodeCompiler compiler)
                {
                    try { runtimeNode = compiler.Compile(new DialectNodeCompilationContext(graph, node, indices, diagnostics)); }
                    catch (System.Exception exception) { diagnostics.Add($"{node.Title}: {exception.Message}"); }
                }
                else diagnostics.Add($"{node.Title}: missing IDialectNodeCompiler implementation.");
                runtimeNode ??= new InvalidRuntimeNode($"{node.Title} could not be compiled.");
                runtimeNode.SetAuthoringId(node.ID.ToString());
                compiled.Add(runtimeNode);
            }

            var starts = authoringNodes.OfType<StartNode>().ToList();
            if (starts.Count != 1) diagnostics.Add($"Expected exactly one Start node, found {starts.Count}.");
            if (!authoringNodes.OfType<EndNode>().Any()) diagnostics.Add("Expected at least one End node.");
            var entry = starts.Count == 1 ? indices[starts[0]] : -1;
            runtime.Configure(graph.AssetGuid.ToString(), entry, compiled, localVariables, blackboards,
                CompileTransitions(authoringNodes, indices), diagnostics);
            AddRuntime(context, runtime);
        }

        static List<DialectVariableDefinition> CompileLocalVariables(DialectGraph graph, ICollection<string> diagnostics)
        {
            var definitions = new List<DialectVariableDefinition>();
            var ids = new HashSet<string>();
            var names = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var variable in graph.GetVariables())
            {
                if (variable == null) continue;
                var id = variable.ID.ToString();
                if (string.IsNullOrWhiteSpace(id) || !ids.Add(id))
                { diagnostics.Add($"Local variable '{variable.Name}' has an invalid or duplicate ID."); continue; }
                if (string.IsNullOrWhiteSpace(variable.Name)) diagnostics.Add("A local variable has an empty name.");
                else if (!names.Add(variable.Name)) diagnostics.Add($"Local variable name '{variable.Name}' is duplicated.");
                if (!DialectValueCompiler.TryCreateValue(variable.DataType, variable, out var value))
                { diagnostics.Add($"Local variable '{variable.Name}' uses unsupported type {variable.DataType.Name}."); continue; }
                definitions.Add(new DialectVariableDefinition(id, variable.Name, value));
            }
            return definitions;
        }

        static List<DialectBlackboard> ValidateBlackboards(IReadOnlyList<DialectBlackboard> source,
            IEnumerable<DialectVariableDefinition> locals, ICollection<string> diagnostics)
        {
            var result = new List<DialectBlackboard>();
            var boards = new HashSet<DialectBlackboard>();
            var ids = new HashSet<string>(locals.Select(variable => variable.Id));
            var names = new HashSet<string>(locals.Select(variable => variable.Name), System.StringComparer.OrdinalIgnoreCase);
            foreach (var board in source)
            {
                if (board == null) { diagnostics.Add("A shared blackboard reference is missing."); continue; }
                if (!boards.Add(board)) { diagnostics.Add($"Shared blackboard '{board.name}' is referenced more than once."); continue; }
                var boardDiagnostics = new List<string>();
                board.GetDiagnostics(boardDiagnostics);
                foreach (var message in boardDiagnostics) diagnostics.Add($"{board.name}: {message}");
                foreach (var variable in board.Variables)
                {
                    if (variable != null && !string.IsNullOrWhiteSpace(variable.Id) && !ids.Add(variable.Id))
                        diagnostics.Add($"Variable ID '{variable.Id}' conflicts between local/shared blackboards.");
                    if (variable != null && !string.IsNullOrWhiteSpace(variable.Name) && !names.Add(variable.Name))
                        diagnostics.Add($"Variable name '{variable.Name}' conflicts between local/shared blackboards.");
                }
                result.Add(board);
            }
            return result;
        }

        static List<DialectTransition> CompileTransitions(IReadOnlyList<DialectNode> nodes,
            IReadOnlyDictionary<INode, int> indices)
        {
            var transitions = new List<DialectTransition>();
            var connected = new List<IPort>();
            for (var i = 0; i < nodes.Count; i++)
            {
                foreach (var output in nodes[i].GetOutputPorts())
                {
                    connected.Clear();
                    output.GetConnectedPorts(connected);
                    foreach (var input in connected)
                        if (indices.TryGetValue(input.GetNode(), out var target))
                            transitions.Add(new DialectTransition(i, target, output.ID.ToString(), input.ID.ToString()));
                }
            }
            return transitions;
        }

        static void AddRuntime(AssetImportContext context, DialectRuntimeGraph runtime)
        {
            context.AddObjectToAsset("RuntimeGraph", runtime);
            context.SetMainObject(runtime);
        }
    }

    [System.Serializable]
    sealed class InvalidRuntimeNode : RuntimeNode
    {
        [SerializeField] string message;
        public InvalidRuntimeNode(string message) => this.message = message;
        public override DialectExecutionResult Execute(Executors.DialectExecutionContext context) =>
            throw new System.InvalidOperationException(message);
    }
}

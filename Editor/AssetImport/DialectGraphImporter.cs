using System.Collections.Generic;
using System.Linq;
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
            var indices = new Dictionary<INode, int>();
            for (var i = 0; i < authoringNodes.Count; i++) indices[authoringNodes[i]] = i;
            var compiled = new List<RuntimeNode>(authoringNodes.Count);
            foreach (var node in authoringNodes)
            {
                RuntimeNode runtimeNode = null;
                if (node is IDialectNodeCompiler compiler)
                {
                    try { runtimeNode = compiler.Compile(new DialectNodeCompilationContext(node, indices, diagnostics)); }
                    catch (System.Exception exception) { diagnostics.Add($"{node.Title}: {exception.Message}"); }
                }
                else diagnostics.Add($"{node.Title}: missing IDialectNodeCompiler implementation.");
                runtimeNode ??= new InvalidRuntimeNode($"{node.Title} could not be compiled.");
                runtimeNode.SetAuthoringId(node.ID.ToString());
                compiled.Add(runtimeNode);
            }

            var starts = authoringNodes.OfType<StartNode>().ToList();
            if (starts.Count != 1) diagnostics.Add($"Expected exactly one Start node, found {starts.Count}.");
            var entry = starts.Count == 1 ? indices[starts[0]] : -1;
            runtime.Configure(graph.AssetGuid.ToString(), entry, compiled, graph.Blackboards.ToList(), diagnostics);
            AddRuntime(context, runtime);
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

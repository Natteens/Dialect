using System.Collections.Generic;
using System.Linq;
using Dialect.Core;
using Dialect.Editor.Nodes;
using Dialect.Editor.Utils;
using Unity.GraphToolkit.Editor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Dialect.Editor.AssetImport
{
    [ScriptedImporter(1, DialectDirectorGraph.AssetExtension)]
    internal class DialectDirectorImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var graph = GraphDatabase.LoadGraphForImporter<DialectDirectorGraph>(ctx.assetPath);
            
            if (graph == null)
            {
                Debug.LogError($"Failed to load Dialect Director graph asset: {ctx.assetPath}");
                return;
            }
            
            var startNodeModel = graph.GetNodes().OfType<StartNode>().FirstOrDefault();
            
            if (startNodeModel == null)
            {
                Debug.LogError($"No start node found in graph: {ctx.assetPath}");
                return;
            }
            
            var runtimeAsset = ScriptableObject.CreateInstance<DialectRuntimeGraph>();
            var nodeMap = new Dictionary<INode, int>();
            
            CreateRuntimeNodes(startNodeModel, runtimeAsset, nodeMap);
            SetupConnections(startNodeModel, runtimeAsset, nodeMap);
            
            ctx.AddObjectToAsset("RuntimeAsset", runtimeAsset);
            ctx.SetMainObject(runtimeAsset);
        }

        void CreateRuntimeNodes(INode startNode, DialectRuntimeGraph runtimeGraph, Dictionary<INode, int> nodeMap)
        {
            var nodesToProcess = new Queue<INode>();
            nodesToProcess.Enqueue(startNode);

            while (nodesToProcess.Count > 0)
            {
                var currentNode = nodesToProcess.Dequeue();
                
                if (nodeMap.ContainsKey(currentNode)) continue;
                
                if (currentNode is IConvertibleToRuntime convertible)
                {
                    var runtimeNode = convertible.CreateRuntimeNode();
                    nodeMap[currentNode] = runtimeGraph.nodes.Count;
                    runtimeGraph.nodes.Add(runtimeNode);
                }
                else
                {
                    Debug.LogWarning($"Node {currentNode.GetType().Name} does not implement IConvertibleToRuntime. Skipping.");
                    continue;
                }
                
                for (int i = 0; i < NodeUtility.GetOutputPortCount(currentNode); i++)
                {
                    var port = currentNode.GetOutputPort(i);

                    if (NodeUtility.IsPortConnected(port))
                    {
                        nodesToProcess.Enqueue(NodeUtility.GetFirstConnectedPort(port).GetNode());
                    }
                }
            }
        }

        void SetupConnections(INode startNode, DialectRuntimeGraph runtimeGraph, Dictionary<INode, int> nodeMap)
        {
            foreach (var kvp in nodeMap)
            {
                var editorNode = kvp.Key;
                var runtimeIndex = kvp.Value;
                var runtimeNode = runtimeGraph.nodes[runtimeIndex];

                for (int i = 0; i < NodeUtility.GetOutputPortCount(editorNode); i++)
                {
                    var port = editorNode.GetOutputPort(i);

                    if (NodeUtility.IsPortConnected(port) && nodeMap.TryGetValue(NodeUtility.GetFirstConnectedPort(port).GetNode(), out int nextIndex))
                    {
                        runtimeNode.nextNodeIndices.Add(nextIndex);
                    }
                }
            }
        }
    }
}

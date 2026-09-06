using System;
using System.Collections.Generic;
using System.Linq;
using Dialect.Blackboards;
using Dialect.Editor.Nodes;
using Unity.GraphToolkit.Editor;
using UnityEngine;

namespace Dialect.Editor
{
    [Serializable, Graph(AssetExtension)]
    public sealed class DialectGraph : Graph
    {
        public const string AssetExtension = "dlg";
        [SerializeField] List<DialectBlackboard> blackboards = new();
        public IReadOnlyList<DialectBlackboard> Blackboards => blackboards;

        public override void OnEnable()
        {
            base.OnEnable();
        }

        public static DialectGraph CreateInitialized(string path)
        {
            var graph = GraphDatabase.CreateGraph<DialectGraph>(path);
            if (graph.NodeCount != 0) return graph;
            graph.UndoBeginRecordGraph("Create dialogue flow");
            var start = new StartNode { Position = new Vector2(120, 180) };
            var end = new EndNode { Position = new Vector2(480, 180) };
            graph.AddNode(start);
            graph.AddNode(end);
            graph.Connect(start.GetOutputPortByName(DialectNode.FlowOutput), end.GetInputPortByName(DialectNode.FlowInput));
            graph.UndoEndRecordGraph();
            GraphDatabase.SaveGraph(graph);
            return graph;
        }

        public override bool IsConnectionAllowed(IPort source, IPort destination)
        {
            if (!base.IsConnectionAllowed(source, destination) || source == null || destination == null) return false;
            return source.GetNode() != destination.GetNode() && source.Direction != destination.Direction &&
                   source.DataType == destination.DataType;
        }

        public override void OnGraphChanged(GraphLogger logger)
        {
            base.OnGraphChanged(logger);
            Validate(logger);
        }

        public void Validate(GraphLogger logger)
        {
            var nodes = GetNodes().ToList();
            var starts = nodes.OfType<StartNode>().ToList();
            var ends = nodes.OfType<EndNode>().ToList();
            if (starts.Count == 0) logger.LogError("The graph needs one Start node.", this);
            else if (starts.Count > 1) logger.LogError("The graph has multiple Start nodes.", this);
            if (ends.Count == 0) logger.LogError("The graph needs at least one End node.", this);

            foreach (var node in nodes.OfType<DialectNode>())
            {
                if (node is IDialectNodeCompiler compiler) compiler.Validate(new DialectNodeValidationContext(node, logger));
                else logger.LogError($"{node.Title} does not implement IDialectNodeCompiler.", node);
            }

            if (starts.Count != 1) return;
            var reachable = FindReachable(starts[0]);
            foreach (var node in nodes.Where(node => !reachable.Contains(node)))
                logger.LogWarning($"{node.Title} is unreachable from Start.", node);
        }

        static HashSet<INode> FindReachable(INode start)
        {
            var found = new HashSet<INode>();
            var pending = new Stack<INode>();
            pending.Push(start);
            while (pending.Count > 0)
            {
                var node = pending.Pop();
                if (!found.Add(node)) continue;
                for (var i = 0; i < node.OutputPortCount; i++)
                {
                    var port = node.GetOutputPort(i);
                    if (port.IsConnected)
                    {
                        var connected = new List<IPort>();
                        port.GetConnectedPorts(connected);
                        foreach (var target in connected) pending.Push(target.GetNode());
                    }
                }
            }
            return found;
        }
    }
}

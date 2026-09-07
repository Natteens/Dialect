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
        [System.NonSerialized] bool strictValidationRequested;
        public IReadOnlyList<DialectBlackboard> Blackboards => blackboards;

        public bool LinkBlackboard(DialectBlackboard blackboard)
        {
            if (blackboard == null || blackboards.Contains(blackboard)) return false;
            blackboards.Add(blackboard);
            return true;
        }

        public bool UnlinkBlackboard(DialectBlackboard blackboard) => blackboards.Remove(blackboard);
        public bool IsBlackboardLinked(DialectBlackboard blackboard) => blackboard != null && blackboards.Contains(blackboard);

        internal void SetBlackboards(IEnumerable<DialectBlackboard> values)
        {
            blackboards.Clear();
            foreach (var value in values)
                if (value != null && !blackboards.Contains(value)) blackboards.Add(value);
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
            foreach (var blackboard in DialectProjectSettings.instance.DefaultBlackboards) graph.LinkBlackboard(blackboard);
            graph.UndoEndRecordGraph();
            GraphDatabase.SaveGraph(graph);
            return graph;
        }

        public override bool IsConnectionAllowed(IPort source, IPort destination)
        {
            if (source == null || destination == null || source.GetNode() == destination.GetNode() || source.Direction == destination.Direction)
                return false;
            if (base.IsConnectionAllowed(source, destination)) return true;
            var input = source.Direction == PortDirection.Input ? source : destination;
            var output = source.Direction == PortDirection.Output ? source : destination;
            if (input.IsConnected) return false;
            if (output.GetNode() is SharedVariableNode shared && shared.TryGetResolvedType(out var sharedType))
            {
                if (input.DataType == typeof(DialectText)) return DialectValueCompiler.IsTextType(sharedType);
                return input.DataType.IsAssignableFrom(sharedType);
            }
            if (input.DataType == typeof(DialectText) && DialectValueCompiler.IsTextType(output.DataType)) return true;
            return input.DataType.IsAssignableFrom(output.DataType);
        }

        public override void OnGraphChanged(GraphLogger logger)
        {
            var mode = strictValidationRequested ? DialectValidationMode.Strict : DialectValidationMode.Live;
            strictValidationRequested = false;
            Validate(logger, mode);
        }

        public void RequestStrictValidation() => strictValidationRequested = true;

        public void Validate(GraphLogger logger) => Validate(logger, DialectValidationMode.Strict);

        public void Validate(GraphLogger logger, DialectValidationMode mode)
        {
            var nodes = GetNodes().ToList();
            var starts = nodes.OfType<StartNode>().ToList();
            var ends = nodes.OfType<EndNode>().ToList();
            if (starts.Count == 0) logger.LogError("The graph needs one Start node.", this,
                new GraphLogAction("Create Start", context => ((DialectGraph)context).CreateStart()));
            else if (starts.Count > 1) logger.LogError("The graph has multiple Start nodes.", this);
            if (ends.Count == 0) logger.LogError("The graph needs at least one End node.", this,
                new GraphLogAction("Create End", context => ((DialectGraph)context).CreateEnd()));

            ValidateBlackboards(logger);

            foreach (var node in nodes.OfType<DialectNode>())
            {
                if (node is IDialectNodeCompiler compiler) compiler.Validate(new DialectNodeValidationContext(this, node, logger, mode));
                else logger.LogError($"{node.Title} does not implement IDialectNodeCompiler.", node);
            }

            foreach (var node in nodes.OfType<DialectValueNode>())
            {
                if (node is IDialectValueNodeCompiler compiler)
                    compiler.Validate(new DialectValueNodeValidationContext(this, node, logger, mode));
                else logger.LogError($"{node.Title} does not implement IDialectValueNodeCompiler.", node);
            }

            if (mode != DialectValidationMode.Strict || starts.Count != 1) return;
            var reachable = FindReachable(starts[0]);
            foreach (var node in nodes.OfType<DialectNode>().Where(node => !reachable.Contains(node)))
                logger.LogWarning($"{node.Title} is unreachable from Start.", node);
            if (HasAutomaticCycle(starts[0])) logger.LogError("The graph contains an automatic flow cycle that can only stop at the runtime step limit.", this);
        }

        void ValidateBlackboards(GraphLogger logger)
        {
            var boards = new HashSet<DialectBlackboard>();
            var ids = new HashSet<string>();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var variable in GetVariables())
            {
                ids.Add(variable.ID.ToString());
                if (!string.IsNullOrWhiteSpace(variable.Name) && !names.Add(variable.Name))
                    logger.LogError($"Local variable name '{variable.Name}' is duplicated.", this);
            }
            foreach (var board in blackboards)
            {
                if (board == null) { logger.LogError("A shared blackboard reference is missing.", this); continue; }
                if (!boards.Add(board))
                {
                    logger.LogError($"Shared blackboard '{board.name}' is referenced more than once.", this,
                        new GraphLogAction("Select Blackboard", _ => UnityEditor.Selection.activeObject = board));
                    continue;
                }
                var messages = new List<string>();
                board.GetDiagnostics(messages);
                foreach (var message in messages)
                    logger.LogError($"{board.name}: {message}", this,
                        new GraphLogAction("Select Blackboard", _ => UnityEditor.Selection.activeObject = board));
                foreach (var variable in board.Variables)
                {
                    if (variable != null && !ids.Add(variable.Id))
                        logger.LogError($"Variable ID '{variable.Id}' conflicts between local/shared blackboards.", this,
                            new GraphLogAction("Select Blackboard", _ => UnityEditor.Selection.activeObject = board));
                    if (variable != null && !string.IsNullOrWhiteSpace(variable.Name) && !names.Add(variable.Name))
                        logger.LogError($"Variable name '{variable.Name}' conflicts between local/shared blackboards.", this,
                            new GraphLogAction("Select Blackboard", _ => UnityEditor.Selection.activeObject = board));
                }
            }
        }

        void CreateStart()
        {
            UndoBeginRecordGraph("Create Start node");
            AddNode(new StartNode { Position = new Vector2(120, 180) });
            UndoEndRecordGraph();
            GraphDatabase.SaveGraph(this);
        }

        void CreateEnd()
        {
            UndoBeginRecordGraph("Create End node");
            AddNode(new EndNode { Position = new Vector2(480, 180) });
            UndoEndRecordGraph();
            GraphDatabase.SaveGraph(this);
        }

        static bool HasAutomaticCycle(INode start)
        {
            var visiting = new HashSet<INode>();
            var visited = new HashSet<INode>();
            return Visit(start, visiting, visited);
        }

        static bool Visit(INode node, ISet<INode> visiting, ISet<INode> visited)
        {
            if (node is IDialectNodeCompiler compiler && compiler.WaitsForInput) return false;
            if (visiting.Contains(node)) return true;
            if (!visited.Add(node)) return false;
            visiting.Add(node);
            var connected = new List<IPort>();
            for (var i = 0; i < node.OutputPortCount; i++)
            {
                connected.Clear();
                node.GetOutputPort(i).GetConnectedPorts(connected);
                foreach (var port in connected)
                    if (port.GetNode() is DialectNode && Visit(port.GetNode(), visiting, visited)) return true;
            }
            visiting.Remove(node);
            return false;
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

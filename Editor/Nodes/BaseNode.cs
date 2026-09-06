using System;
using System.Collections.Generic;
using Dialect.Core;
using Dialect.Editor.Utils;
using Unity.GraphToolkit.Editor;

namespace Dialect.Editor.Nodes
{
    [Serializable]
    public abstract class DialectNode : Node
    {
        public const string FlowInput = "flowIn";
        public const string FlowOutput = "flowOut";
        protected static void AddFlowInput(IPortDefinitionContext context, string name = FlowInput, string label = "In") =>
            context.AddInputPort(name).WithDisplayName(label).WithConnectorUI(PortConnectorUI.Arrowhead)
                .WithCapacity(PortCapacity.Multi).Build();
        protected static void AddFlowOutput(IPortDefinitionContext context, string name = FlowOutput, string label = "Out") =>
            context.AddOutputPort(name).WithDisplayName(label).WithConnectorUI(PortConnectorUI.Arrowhead)
                .WithCapacity(PortCapacity.Single).Build();
        protected static void AddValueInput<T>(IPortDefinitionContext context, string name, string label) =>
            context.AddInputPort<T>(name).WithDisplayName(label).WithConnectorUI(PortConnectorUI.Circle)
                .WithCapacity(PortCapacity.Single).Build();
    }

    public interface IDialectNodeCompiler
    {
        RuntimeNode Compile(DialectNodeCompilationContext context);
        void Validate(DialectNodeValidationContext context);
        bool WaitsForInput { get; }
    }

    public sealed class DialectNodeCompilationContext
    {
        readonly IReadOnlyDictionary<INode, int> indices;
        readonly List<string> diagnostics;
        internal DialectNodeCompilationContext(DialectNode node, IReadOnlyDictionary<INode, int> indices,
            List<string> diagnostics) { Node = node; this.indices = indices; this.diagnostics = diagnostics; }
        public DialectNode Node { get; }
        public T Read<T>(string portName) => NodeUtility.GetInputPortValue<T>(Node.GetInputPortByName(portName));
        public int Target(string portName, bool required = true)
        {
            var port = Node.GetOutputPortByName(portName);
            if (port != null && port.IsConnected && port.FirstConnectedPort != null &&
                indices.TryGetValue(port.FirstConnectedPort.GetNode(), out var index)) return index;
            if (required) Error($"Required output '{portName}' is not connected.");
            return -1;
        }
        public void Error(string message) => diagnostics.Add($"{Node.Title}: {message}");
    }

    public sealed class DialectNodeValidationContext
    {
        readonly GraphLogger logger;
        internal DialectNodeValidationContext(DialectNode node, GraphLogger logger) { Node = node; this.logger = logger; }
        public DialectNode Node { get; }
        public bool IsConnected(string portName)
        {
            var port = Node.GetInputPortByName(portName) ?? Node.GetOutputPortByName(portName);
            return port != null && port.IsConnected;
        }
        public T Read<T>(string portName) => NodeUtility.GetInputPortValue<T>(Node.GetInputPortByName(portName));
        public void Error(string message) => logger.LogError(message, Node);
        public void Warning(string message) => logger.LogWarning(message, Node);
    }
}

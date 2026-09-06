using System;
using Dialect.Core;
using Dialect.Nodes;
using Unity.GraphToolkit.Editor;

namespace Dialect.Editor.Nodes
{
    [Serializable]
    [Node("Dialogue/Flow", null, "Start")]
    public sealed class StartNode : DialectNode, IDialectNodeCompiler
    {
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            DefaultColor = new UnityEngine.Color(.16f, .64f, .6f);
            Subtitle = "Entry";
            AddFlowOutput(context);
        }

        public RuntimeNode Compile(DialectNodeCompilationContext context) => new StartRuntimeNode(context.Target(FlowOutput));
        public void Validate(DialectNodeValidationContext context)
        { if (!context.IsConnected(FlowOutput)) context.Error("Start must be connected."); }
        public bool WaitsForInput => false;
    }
}

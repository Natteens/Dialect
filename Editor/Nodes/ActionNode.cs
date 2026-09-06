using System;
using Dialect.Actions;
using Dialect.Core;
using Dialect.Nodes;
using Unity.GraphToolkit.Editor;

namespace Dialect.Editor.Nodes
{
    [Serializable]
    [Node("Dialogue/Logic", null, "Action")]
    public sealed class ActionNode : DialectNode, IDialectNodeCompiler
    {
        const string ACTION_PORT = "action";

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            DefaultColor = new UnityEngine.Color(.3f, .62f, .36f);
            Subtitle = "Game action";
            AddFlowInput(context);
            AddValueInput<DialectAction>(context, ACTION_PORT, "Action");
            AddFlowOutput(context);
        }

        public RuntimeNode Compile(DialectNodeCompilationContext context) =>
            new ActionRuntimeNode(context.Read<DialectAction>(ACTION_PORT), context.Target(FlowOutput));
        public void Validate(DialectNodeValidationContext context)
        {
            if (context.Read<DialectAction>(ACTION_PORT) == null) context.Error("Assign an action.");
            if (!context.IsConnected(FlowOutput)) context.Error("Action must continue to another node.");
        }
        public bool WaitsForInput => false;
    }
}

using System;
using Dialect.Actions;
using Dialect.Core;
using Dialect.Nodes;
using Unity.GraphToolkit.Editor;

namespace Dialect.Editor.Nodes
{
    [Serializable]
    [Node("Logic", null, "Action", "Packages/com.natteens.dialect/Editor/Styles/DialectNodes.uss")]
    public sealed class ActionNode : DialectNode, IDialectNodeCompiler
    {
        const string ACTION_PORT = "action";

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            DefaultColor = new UnityEngine.Color(.3f, .62f, .36f);
            Subtitle = "Game action";
            AddFlowInput(context);
            AddValueInput<DialectAction>(context, ACTION_PORT, "Action", "Reusable DialectAction asset. None is allowed while authoring and reported by strict validation.");
            AddFlowOutput(context);
        }

        public RuntimeNode Compile(DialectNodeCompilationContext context)
        {
            var action = context.Read<DialectAction>(ACTION_PORT);
            if (action == null) context.Error("Assign an action.");
            return new ActionRuntimeNode(action, context.Target(FlowOutput));
        }
        public void Validate(DialectNodeValidationContext context)
        {
            if (!context.IsStrict) return;
            if (context.Read<DialectAction>(ACTION_PORT) == null) context.Error("Assign an action.");
            if (!context.IsConnected(FlowOutput)) context.Error("Action must continue to another node.");
        }
        public bool WaitsForInput => false;
    }
}

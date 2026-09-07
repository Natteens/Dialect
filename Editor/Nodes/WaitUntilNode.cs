using System;
using Dialect.Core;
using Dialect.Nodes;
using Unity.GraphToolkit.Editor;

namespace Dialect.Editor.Nodes
{
    [Serializable, Node("Flow", null, "Wait Until", "Packages/com.natteens.dialect/Editor/Styles/DialectNodes.uss")]
    public sealed class WaitUntilNode : DialectNode, IDialectNodeCompiler
    {
        public const string ConditionPort = "condition";

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            DefaultColor = new UnityEngine.Color(.47f, .38f, .7f);
            Subtitle = "Continue when true";
            Tooltip = "Re-evaluates a Boolean value once per frame while this session is suspended.";
            AddFlowInput(context);
            AddValueInput<bool>(context, ConditionPort, "Condition", "Boolean value checked until it becomes true.");
            AddFlowOutput(context);
        }

        public RuntimeNode Compile(DialectNodeCompilationContext context) =>
            new WaitUntilRuntimeNode(context.CompileValue(ConditionPort, typeof(bool)), context.Target(FlowOutput));

        public void Validate(DialectNodeValidationContext context)
        {
            if (context.IsStrict && !context.IsConnected(FlowOutput))
                context.Error("Wait Until must continue to another node.");
        }

        public bool WaitsForInput => false;
    }
}

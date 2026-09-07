using System;
using Dialect.Core;
using Dialect.Nodes;
using Unity.GraphToolkit.Editor;

namespace Dialect.Editor.Nodes
{
    [Serializable, Node("Flow", null, "Branch", "Packages/com.natteens.dialect/Editor/Styles/DialectNodes.uss")]
    public sealed class BranchNode : DialectNode, IDialectNodeCompiler
    {
        public const string ConditionPort = "condition";
        public const string TruePort = "true";
        public const string FalsePort = "false";

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            DefaultColor = new UnityEngine.Color(.56f, .35f, .73f);
            Subtitle = "Boolean";
            Tooltip = "Routes flow using a bool value from this graph or a connected value node.";
            AddFlowInput(context);
            AddValueInput<bool>(context, ConditionPort, "Condition", "Boolean value that selects the True or False path.");
            AddFlowOutput(context, TruePort, "True");
            AddFlowOutput(context, FalsePort, "False");
        }

        public RuntimeNode Compile(DialectNodeCompilationContext context) => new BranchRuntimeNode(
            context.CompileValue(ConditionPort, typeof(bool)), context.Target(TruePort), context.Target(FalsePort));

        public void Validate(DialectNodeValidationContext context)
        {
            if (!context.IsStrict) return;
            if (!context.IsConnected(TruePort)) context.Error("Connect the True output.");
            if (!context.IsConnected(FalsePort)) context.Error("Connect the False output.");
        }

        public bool WaitsForInput => false;
    }
}

using System;
using Dialect.Conditions;
using Dialect.Core;
using Dialect.Nodes;
using Unity.GraphToolkit.Editor;

namespace Dialect.Editor.Nodes
{
    [Serializable]
    [Node("Dialogue/Logic", null, "Condition")]
    public sealed class ConditionNode : DialectNode, IDialectNodeCompiler
    {
        const string CONDITION_PORT = "condition";
        const string TRUE_PORT = "true";
        const string FALSE_PORT = "false";

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            DefaultColor = new UnityEngine.Color(.56f, .35f, .73f);
            Subtitle = "Branch";
            AddFlowInput(context);
            AddValueInput<DialectCondition>(context, CONDITION_PORT, "Condition");
            AddFlowOutput(context, TRUE_PORT, "True");
            AddFlowOutput(context, FALSE_PORT, "False");
        }

        public RuntimeNode Compile(DialectNodeCompilationContext context) => new ConditionRuntimeNode(
            context.Read<DialectCondition>(CONDITION_PORT), context.Target(TRUE_PORT), context.Target(FALSE_PORT));
        public void Validate(DialectNodeValidationContext context)
        {
            if (context.Read<DialectCondition>(CONDITION_PORT) == null) context.Error("Assign a condition.");
            if (!context.IsConnected(TRUE_PORT)) context.Error("Connect the True output.");
            if (!context.IsConnected(FALSE_PORT)) context.Error("Connect the False output.");
        }
        public bool WaitsForInput => false;
    }
}

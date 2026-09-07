using System;
using Dialect.Core;
using Dialect.Nodes;
using Unity.GraphToolkit.Editor;

namespace Dialect.Editor.Nodes
{
    [Serializable, Node("Flow", null, "Wait For Resume", "Packages/com.natteens.dialect/Editor/Styles/DialectNodes.uss")]
    public sealed class WaitForResumeNode : DialectNode, IDialectNodeCompiler
    {
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            DefaultColor = new UnityEngine.Color(.47f, .38f, .7f);
            Subtitle = "External resume";
            Tooltip = "Suspends until consumer code calls Resume on the Dialect Director.";
            AddFlowInput(context);
            AddFlowOutput(context);
        }

        public RuntimeNode Compile(DialectNodeCompilationContext context) =>
            new WaitForResumeRuntimeNode(context.Target(FlowOutput));

        public void Validate(DialectNodeValidationContext context)
        {
            if (context.IsStrict && !context.IsConnected(FlowOutput))
                context.Error("Wait For Resume must continue to another node.");
        }

        public bool WaitsForInput => true;
    }
}

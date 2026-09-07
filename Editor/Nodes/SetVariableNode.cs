using System;
using Dialect.Blackboards;
using Dialect.Core;
using Dialect.Nodes;
using Unity.GraphToolkit.Editor;

namespace Dialect.Editor.Nodes
{
    [Serializable, Node("Logic", null, "Set Variable", "Packages/com.natteens.dialect/Editor/Styles/DialectNodes.uss")]
    public sealed class SetVariableNode : DialectNode, IDialectNodeCompiler
    {
        public const string TargetOption = "target";
        public const string ValuePort = "value";

        protected override void OnDefineOptions(IOptionDefinitionContext context) =>
            context.AddOption<DialectVariableTarget>(TargetOption).WithDisplayName("Variable")
                .WithTooltip("Session variable to change. Assets are never modified.");

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            DefaultColor = new UnityEngine.Color(.32f, .6f, .43f);
            Subtitle = "Session value";
            AddFlowInput(context);
            var target = ReadTarget();
            var type = target.TryResolve(Graph as DialectGraph, out _, out var resolvedType, out _) ? resolvedType : typeof(string);
            context.AddInputPort(ValuePort).WithDataType(type).WithDisplayName("Value")
                .WithTooltip("New value copied into this dialogue session.").WithConnectorUI(PortConnectorUI.Circle)
                .WithCapacity(PortCapacity.Single).Build();
            AddFlowOutput(context);
        }

        public RuntimeNode Compile(DialectNodeCompilationContext context)
        {
            var target = ReadTarget();
            if (!target.TryResolve(context.Graph, out var id, out var type, out var valueType))
            {
                context.Error("Select a valid local or shared variable.");
                id = string.Empty;
                type = typeof(string);
                valueType = DialectValueType.String;
            }
            var expression = context.CompileValue(ValuePort, type);
            return new SetVariableRuntimeNode(id, valueType, expression, context.Target(FlowOutput));
        }

        public void Validate(DialectNodeValidationContext context)
        {
            if (!context.IsStrict) return;
            if (!ReadTarget().TryResolve(context.Graph, out _, out _, out _)) context.Error("Select a valid local or shared variable.");
            if (!context.IsConnected(FlowOutput)) context.Error("Set Variable must continue to another node.");
        }

        public bool WaitsForInput => false;
        DialectVariableTarget ReadTarget()
        {
            var target = default(DialectVariableTarget);
            GetNodeOptionByName(TargetOption)?.TryGetValue(out target);
            return target;
        }
    }
}

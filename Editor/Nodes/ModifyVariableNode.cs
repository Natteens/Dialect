using System;
using Dialect.Blackboards;
using Dialect.Core;
using Dialect.Nodes;
using Unity.GraphToolkit.Editor;

namespace Dialect.Editor.Nodes
{
    [Serializable, Node("Logic", null, "Modify Variable", "Packages/com.natteens.dialect/Editor/Styles/DialectNodes.uss")]
    public sealed class ModifyVariableNode : DialectNode, IDialectNodeCompiler
    {
        public const string SettingsOption = "settings";
        public const string OperandPort = "operand";

        protected override void OnDefineOptions(IOptionDefinitionContext context) =>
            context.AddOption<DialectModifySettings>(SettingsOption).WithDisplayName("Modification")
                .WithTooltip("Choose a session variable and an operation valid for its type.");

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            DefaultColor = new UnityEngine.Color(.32f, .6f, .43f);
            AddFlowInput(context);
            if (TryReadTarget(out var type, out var valueType))
            {
                Subtitle = valueType switch
                {
                    DialectValueType.Boolean => "Toggle session value",
                    DialectValueType.String => "Append to session value",
                    _ => "Modify session value"
                };
                if (valueType is DialectValueType.Integer or DialectValueType.Float or DialectValueType.String)
                    context.AddInputPort(OperandPort).WithDataType(type).WithDisplayName("Operand")
                        .WithTooltip("Typed value used by this operation.").WithConnectorUI(PortConnectorUI.Circle)
                        .WithCapacity(PortCapacity.Single).Build();
            }
            else Subtitle = "Choose a supported variable";
            AddFlowOutput(context);
        }

        public RuntimeNode Compile(DialectNodeCompilationContext context)
        {
            var target = ReadSettings().Target;
            if (!target.TryResolve(context.Graph, out var id, out var type, out var valueType) || !IsSupported(valueType))
            {
                context.Error("Select a String, Boolean, Integer, or Float session variable.");
                id = string.Empty;
                type = typeof(string);
                valueType = DialectValueType.String;
            }
            var operand = valueType == DialectValueType.Boolean ? default : context.CompileValue(OperandPort, type);
            return new ModifyVariableRuntimeNode(id, valueType, ReadSettings().NumericOperation, operand, context.Target(FlowOutput));
        }

        public void Validate(DialectNodeValidationContext context)
        {
            if (!context.IsStrict) return;
            if (!ReadSettings().Target.TryResolve(context.Graph, out _, out _, out var valueType) || !IsSupported(valueType))
                context.Error("Select a String, Boolean, Integer, or Float session variable.");
            if (!context.IsConnected(FlowOutput)) context.Error("Modify Variable must continue to another node.");
        }

        public bool WaitsForInput => false;

        bool TryReadTarget(out Type type, out DialectValueType valueType) =>
            ReadSettings().Target.TryResolve(Graph as DialectGraph, out _, out type, out valueType) && IsSupported(valueType);

        DialectModifySettings ReadSettings()
        {
            var settings = default(DialectModifySettings);
            GetNodeOptionByName(SettingsOption)?.TryGetValue(out settings);
            return settings;
        }

        static bool IsSupported(DialectValueType type) => type is DialectValueType.String or
            DialectValueType.Boolean or DialectValueType.Integer or DialectValueType.Float;
    }
}

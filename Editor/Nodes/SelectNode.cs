using System;
using Dialect.Blackboards;
using Dialect.Values;
using Unity.GraphToolkit.Editor;

namespace Dialect.Editor.Nodes
{
    [Serializable, Node("Values", null, "Select", "Packages/com.natteens.dialect/Editor/Styles/DialectNodes.uss")]
    public sealed class SelectNode : DialectValueNode, IDialectValueNodeCompiler
    {
        public const string TypeOption = "type";
        public const string ConditionPort = "condition";
        public const string TruePort = "trueValue";
        public const string FalsePort = "falseValue";

        protected override void OnDefineOptions(IOptionDefinitionContext context) =>
            context.AddOption<DialectValueType>(TypeOption).WithDisplayName("Value Type")
                .WithTooltip("Controls the typed True, False, and Value ports.");

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            DefaultColor = new UnityEngine.Color(.42f, .55f, .72f);
            Subtitle = "Condition ? True : False";
            Tooltip = "Returns exactly one of two typed values.";
            var type = ValueType;
            AddValueInput<bool>(context, ConditionPort, "Condition");
            context.AddInputPort(TruePort).WithDataType(type).WithDisplayName("True").WithConnectorUI(PortConnectorUI.Circle)
                .WithCapacity(PortCapacity.Single).Build();
            context.AddInputPort(FalsePort).WithDataType(type).WithDisplayName("False").WithConnectorUI(PortConnectorUI.Circle)
                .WithCapacity(PortCapacity.Single).Build();
            context.AddOutputPort(ValueOutput).WithDataType(type).WithDisplayName("Value").WithConnectorUI(PortConnectorUI.Circle)
                .WithCapacity(PortCapacity.Multi).Build();
        }

        public Type ValueType => DialectValueUtility.GetSystemType(ReadType());

        public DialectValueResolver CompileValue(DialectValueNodeCompilationContext context)
        {
            var type = ReadType();
            if (!Enum.IsDefined(typeof(DialectValueType), type)) context.Error("Select a supported Value Type.");
            return new SelectValueResolver(type, context.CompileValue(ConditionPort, typeof(bool)),
                context.CompileValue(TruePort, ValueType), context.CompileValue(FalsePort, ValueType));
        }

        public void Validate(DialectValueNodeValidationContext context)
        {
            if (context.IsStrict && !Enum.IsDefined(typeof(DialectValueType), ReadType()))
                context.Error("Select a supported Value Type.");
        }

        DialectValueType ReadType()
        {
            var value = DialectValueType.String;
            GetNodeOptionByName(TypeOption)?.TryGetValue(out value);
            return value;
        }
    }
}

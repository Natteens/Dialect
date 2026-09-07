using System;
using Dialect.Values;
using Unity.GraphToolkit.Editor;

namespace Dialect.Editor.Nodes
{
    [Serializable, Node("Values", null, "Compare", "Packages/com.natteens.dialect/Editor/Styles/DialectNodes.uss")]
    public sealed class CompareNode : DialectValueNode, IDialectValueNodeCompiler
    {
        public const string SettingsOption = "settings";
        public const string LeftPort = "left";
        public const string RightPort = "right";

        protected override void OnDefineOptions(IOptionDefinitionContext context) =>
            context.AddOption<DialectCompareSettings>(SettingsOption).WithDisplayName("Comparison")
                .WithTooltip("Choose a type and one of its meaningful operators.")
                .WithDefaultValue(new DialectCompareSettings(DialectCompareType.String, DialectComparisonOperator.Equal));

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            DefaultColor = new UnityEngine.Color(.42f, .55f, .72f);
            var settings = ReadSettings();
            Subtitle = OperatorLabel(settings.Operator);
            Tooltip = "Compares two typed values. Float equality is exact.";
            var type = SystemType(settings.Type);
            context.AddInputPort(LeftPort).WithDataType(type).WithDisplayName("A").WithConnectorUI(PortConnectorUI.Circle)
                .WithCapacity(PortCapacity.Single).Build();
            context.AddInputPort(RightPort).WithDataType(type).WithDisplayName("B").WithConnectorUI(PortConnectorUI.Circle)
                .WithCapacity(PortCapacity.Single).Build();
            AddValueOutput<bool>(context, ValueOutput, "Result");
        }

        public Type ValueType => typeof(bool);

        public DialectValueResolver CompileValue(DialectValueNodeCompilationContext context)
        {
            var settings = ReadSettings();
            if (!Enum.IsDefined(typeof(DialectCompareType), settings.Type) ||
                !CompareValueResolver.IsSupported(settings.Type, settings.RawOperator))
                context.Error("Select an operator supported by the comparison type.");
            var type = SystemType(settings.Type);
            return new CompareValueResolver(settings.Type, settings.Operator,
                context.CompileValue(LeftPort, type), context.CompileValue(RightPort, type));
        }

        public void Validate(DialectValueNodeValidationContext context)
        {
            if (!context.IsStrict) return;
            var settings = ReadSettings();
            if (!Enum.IsDefined(typeof(DialectCompareType), settings.Type) ||
                !CompareValueResolver.IsSupported(settings.Type, settings.RawOperator))
                context.Error("Select an operator supported by the comparison type.");
        }

        DialectCompareSettings ReadSettings()
        {
            var value = new DialectCompareSettings(DialectCompareType.String, DialectComparisonOperator.Equal);
            GetNodeOptionByName(SettingsOption)?.TryGetValue(out value);
            return value;
        }

        static Type SystemType(DialectCompareType type) => type switch
        {
            DialectCompareType.Boolean => typeof(bool),
            DialectCompareType.Integer => typeof(int),
            DialectCompareType.Float => typeof(float),
            DialectCompareType.Object => typeof(UnityEngine.Object),
            _ => typeof(string)
        };

        static string OperatorLabel(DialectComparisonOperator value) => value switch
        {
            DialectComparisonOperator.NotEqual => "A is not equal to B",
            DialectComparisonOperator.Less => "A is less than B",
            DialectComparisonOperator.LessOrEqual => "A is less than or equal to B",
            DialectComparisonOperator.Greater => "A is greater than B",
            DialectComparisonOperator.GreaterOrEqual => "A is greater than or equal to B",
            _ => "A is equal to B"
        };
    }
}

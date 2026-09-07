using System;
using Dialect.Core;
using Dialect.Nodes;
using Unity.GraphToolkit.Editor;

namespace Dialect.Editor.Nodes
{
    [Serializable, Node("Flow", null, "Wait", "Packages/com.natteens.dialect/Editor/Styles/DialectNodes.uss")]
    public sealed class WaitNode : DialectNode, IDialectNodeCompiler
    {
        public const string TimeModeOption = "timeMode";
        public const string DurationPort = "duration";

        protected override void OnDefineOptions(IOptionDefinitionContext context) =>
            context.AddOption<WaitTimeMode>(TimeModeOption).WithDisplayName("Time Mode")
                .WithTooltip("Scaled follows Time.timeScale. Unscaled uses real frame time.");

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            DefaultColor = new UnityEngine.Color(.47f, .38f, .7f);
            Subtitle = "Pause, then continue";
            Tooltip = "Suspends this dialogue session for a duration without blocking the main thread.";
            AddFlowInput(context);
            AddValueInput<float>(context, DurationPort, "Duration", "Seconds to wait. Zero or negative continues immediately.");
            AddFlowOutput(context);
        }

        public RuntimeNode Compile(DialectNodeCompilationContext context) =>
            new WaitRuntimeNode(context.CompileValue(DurationPort, typeof(float)), ReadTimeMode(), context.Target(FlowOutput));

        public void Validate(DialectNodeValidationContext context)
        {
            if (!context.IsStrict) return;
            if (!context.IsConnected(DurationPort))
            {
                var duration = context.Read<float>(DurationPort);
                if (float.IsNaN(duration) || float.IsInfinity(duration)) context.Error("Duration must be a finite number.");
            }
            if (ReadTimeMode() is not WaitTimeMode.Scaled and not WaitTimeMode.Unscaled)
                context.Error("Select a valid Time Mode.");
            if (!context.IsConnected(FlowOutput)) context.Error("Wait must continue to another node.");
        }

        public bool WaitsForInput => false;

        WaitTimeMode ReadTimeMode()
        {
            var value = WaitTimeMode.Scaled;
            GetNodeOptionByName(TimeModeOption)?.TryGetValue(out value);
            return value;
        }
    }
}

using System;
using System.Collections.Generic;
using Dialect.Core;
using Dialect.Nodes;
using Unity.GraphToolkit.Editor;

namespace Dialect.Editor.Nodes
{
    [Serializable, Node("Flow", null, "Random Branch", "Packages/com.natteens.dialect/Editor/Styles/DialectNodes.uss")]
    public sealed class RandomBranchNode : DialectNode, IDialectNodeCompiler
    {
        public const string CountOption = "branches";

        protected override void OnDefineOptions(IOptionDefinitionContext context) =>
            context.AddOption<DialectPortCount>(CountOption).WithDisplayName("Outputs")
                .WithTooltip("Add or remove uniform random outputs.").WithDefaultValue(new DialectPortCount(2));

        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            DefaultColor = new UnityEngine.Color(.47f, .38f, .7f);
            Subtitle = "Deterministic session random";
            AddFlowInput(context);
            for (var i = 0; i < GetCount(); i++) AddFlowOutput(context, OutputName(i), $"Output {i + 1}");
        }

        public RuntimeNode Compile(DialectNodeCompilationContext context)
        {
            var targets = new List<int>();
            for (var i = 0; i < GetCount(); i++) targets.Add(context.Target(OutputName(i)));
            return new RandomBranchRuntimeNode(targets);
        }

        public void Validate(DialectNodeValidationContext context)
        {
            if (!context.IsStrict) return;
            for (var i = 0; i < GetCount(); i++)
                if (!context.IsConnected(OutputName(i))) context.Error($"Connect Output {i + 1}.");
        }

        public bool WaitsForInput => false;
        int GetCount()
        {
            var value = new DialectPortCount(2);
            GetNodeOptionByName(CountOption)?.TryGetValue(out value);
            return value.Count;
        }
        public static string OutputName(int index) => $"random{index}";
    }
}

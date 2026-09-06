using System;
using System.Collections.Generic;
using Dialect.Blackboards;
using Dialect.Core;
using Dialect.Nodes;
using Unity.GraphToolkit.Editor;

namespace Dialect.Editor.Nodes
{
    [Serializable, Node("Dialogue", null, "Choice")]
    public sealed class ChoiceNode : DialectNode, IDialectNodeCompiler
    {
        const string CountOption = "choiceCount";
        const string Prefix = "choice";
        protected override void OnDefineOptions(IOptionDefinitionContext context) =>
            context.AddOption(CountOption, typeof(int)).WithDisplayName("Choice Count")
                .WithTooltip("Number of choices shown to the player.").WithDefaultValue(2);
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            DefaultColor = new UnityEngine.Color(.84f, .48f, .18f);
            Subtitle = "Player choice";
            AddFlowInput(context);
            var count = GetCount();
            for (var i = 0; i < count; i++)
            {
                AddValueInput<DialectText>(context, TextName(i), $"Choice {i + 1}");
                AddFlowOutput(context, TargetName(i), $"Choice {i + 1}");
            }
        }
        public RuntimeNode Compile(DialectNodeCompilationContext context)
        {
            var choices = new List<DialectChoiceDefinition>();
            for (var i = 0; i < GetCount(); i++)
                choices.Add(new DialectChoiceDefinition(context.Read<DialectText>(TextName(i)), context.Target(TargetName(i))));
            return new ChoiceRuntimeNode(choices);
        }
        public void Validate(DialectNodeValidationContext context)
        {
            if (GetCount() < 1) context.Error("Add at least one choice.");
            for (var i = 0; i < GetCount(); i++)
                if (!context.IsConnected(TargetName(i))) context.Error($"Connect Choice {i + 1}.");
        }
        public bool WaitsForInput => true;
        int GetCount() { var count = 2; GetNodeOptionByName(CountOption)?.TryGetValue(out count); return Math.Clamp(count, 1, 8); }
        static string TextName(int index) => $"{Prefix}{index}Text";
        static string TargetName(int index) => $"{Prefix}{index}Target";
    }
}

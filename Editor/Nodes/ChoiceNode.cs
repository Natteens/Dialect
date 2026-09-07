using System;
using System.Collections.Generic;
using Dialect.Blackboards;
using Dialect.Core;
using Dialect.Nodes;
using Unity.GraphToolkit.Editor;

namespace Dialect.Editor.Nodes
{
    [Serializable, Node("Dialogue", null, "Choice", "Packages/com.natteens.dialect/Editor/Styles/DialectNodes.uss")]
    public sealed class ChoiceNode : DialectNode, IDialectNodeCompiler
    {
        public const string CountOption = "choiceCount";
        const string Prefix = "choice";
        protected override void OnDefineOptions(IOptionDefinitionContext context) =>
            context.AddOption<DialectPortCount>(CountOption).WithDisplayName("Choices")
                .WithTooltip("Add or remove choices without remapping existing outputs.")
                .WithDefaultValue(new DialectPortCount(2));
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            DefaultColor = new UnityEngine.Color(.84f, .48f, .18f);
            Subtitle = "Player choice";
            AddFlowInput(context);
            var count = GetCount();
            for (var i = 0; i < count; i++)
            {
                AddValueInput<DialectText>(context, TextName(i), $"Choice {i + 1}", "Inline or Localized choice text; a wire overrides the default.");
                AddFlowOutput(context, TargetName(i), $"Choice {i + 1}");
            }
        }
        public RuntimeNode Compile(DialectNodeCompilationContext context)
        {
            var choices = new List<DialectChoiceDefinition>();
            for (var i = 0; i < GetCount(); i++)
                choices.Add(new DialectChoiceDefinition(context.ReadRequiredText(TextName(i), $"Choice {i + 1}"), context.Target(TargetName(i))));
            return new ChoiceRuntimeNode(choices);
        }
        public void Validate(DialectNodeValidationContext context)
        {
            if (!context.IsStrict) return;
            if (GetCount() < 1) context.Error("Add at least one choice.");
            for (var i = 0; i < GetCount(); i++)
            {
                context.ValidateText(TextName(i), $"Choice {i + 1}");
                if (!context.IsConnected(TargetName(i))) context.Error($"Connect Choice {i + 1}.");
            }
        }
        public bool WaitsForInput => true;
        int GetCount()
        {
            var value = new DialectPortCount(2);
            GetNodeOptionByName(CountOption)?.TryGetValue(out value);
            return value.Count;
        }
        public static string TextName(int index) => $"{Prefix}{index}Text";
        public static string TargetName(int index) => $"{Prefix}{index}Target";
    }
}
